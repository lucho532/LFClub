using System.Security.Cryptography;
using System.Text;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Pagos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Ayudas para las pruebas del cobro de la plataforma (spec 017): fija tarifas y mes de inicio, crea
/// cobros e intentos directamente en la base, dispara la generación y la suspensión sin esperar al
/// trabajo en segundo plano y arma notificaciones firmadas con los secretos inventados de la fábrica.
/// </summary>
public class SembradorCobrosPlataforma
{
    public const string SecretoDeEventos = "secreto-de-eventos-de-prueba";
    public const string SecretoDeIntegridad = "secreto-de-integridad-de-prueba";
    public const string LlavePublica = "llave-publica-de-prueba";

    private readonly FabricaApi _fabrica;

    public SembradorCobrosPlataforma(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Guarda las tarifas; sin ningún precio, no se genera ningún cobro.</summary>
    public Task FijarTarifasAsync(
        decimal? jugador = null, decimal? entrenador = null, decimal? directivo = null, decimal? presidente = null, int dias = 5) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var fila = await contexto.ConfiguracionPlataforma.SingleAsync();
            fila.PrecioJugador = jugador;
            fila.PrecioEntrenador = entrenador;
            fila.PrecioDirectivo = directivo;
            fila.PrecioPresidente = presidente;
            fila.DiasDeGracia = dias;
            await contexto.SaveChangesAsync();
            return true;
        });

    /// <summary>Vuelve las tarifas al estado inicial: sin precios y cinco días de gracia.</summary>
    public Task QuitarTarifasAsync() => FijarTarifasAsync();

    /// <summary>Fija el mes de inicio del cobro de un club.</summary>
    public Task FijarInicioAsync(Club club, DateOnly? mes) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.Clubes.Where(c => c.Id == club.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.MesInicioCobro, mes));
            return true;
        });

    /// <summary>Crea un cobro directamente en la base.</summary>
    public Task<CobroPlataforma> CrearCobroAsync(
        Club club,
        DateOnly periodo,
        DateOnly suspendeDesde,
        decimal total = 50000,
        EstadoCobroPlataforma estado = EstadoCobroPlataforma.PENDIENTE,
        int jugadores = 10) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var cobro = new CobroPlataforma
            {
                ClubId = club.Id,
                Periodo = periodo,
                Estado = estado,
                Jugadores = jugadores,
                PrecioJugador = total / jugadores,
                Total = total,
                GeneradoEn = DateTime.UtcNow,
                DiasDeGracia = 5,
                SuspendeDesde = suspendeDesde,
            };
            contexto.CobrosPlataforma.Add(cobro);
            await contexto.SaveChangesAsync();
            return cobro;
        });

    /// <summary>Crea un intento de pago de un cobro, como el que crea el PRESIDENTE, y devuelve su referencia.</summary>
    public Task<IntentoPagoPlataforma> CrearIntentoAsync(CobroPlataforma cobro) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var intento = new IntentoPagoPlataforma
            {
                CobroId = cobro.Id,
                Referencia = "LFC-" + Guid.NewGuid().ToString("N"),
                Valor = cobro.Total,
                CreadoEn = DateTime.UtcNow,
            };
            contexto.IntentosPagoPlataforma.Add(intento);
            await contexto.SaveChangesAsync();
            return intento;
        });

    /// <summary>Los cobros de un club, el más reciente primero.</summary>
    public Task<List<CobroPlataforma>> CobrosDeAsync(Club club) =>
        _fabrica.ConContextoAsync(contexto => contexto.CobrosPlataforma.AsNoTracking()
            .Where(c => c.ClubId == club.Id).OrderByDescending(c => c.Periodo).ToListAsync());

    /// <summary>El club tal como está guardado.</summary>
    public Task<Club> ClubAsync(Club club) =>
        _fabrica.ConContextoAsync(contexto => contexto.Clubes.AsNoTracking().SingleAsync(c => c.Id == club.Id));

    /// <summary>Genera los cobros del mes con el reloj de la fábrica. Devuelve cuántos creó.</summary>
    public async Task<int> GenerarAsync()
    {
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioCobrosPlataforma>().GenerarAsync();
    }

    /// <summary>Aplica la suspensión por impago con el reloj de la fábrica. Devuelve cuántos clubes suspendió.</summary>
    public async Task<int> AplicarSuspensionAsync()
    {
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioSuspensionPorImpago>().AplicarAsync();
    }

    /// <summary>
    /// Arma el cuerpo y el checksum de una notificación <c>transaction.updated</c> firmada con el
    /// secreto de eventos de las pruebas.
    /// </summary>
    public static (string Cuerpo, string Checksum) Notificacion(
        string referencia, decimal valorEnPesos, string estado = "APPROVED", string? transaccionId = null, string medio = "PSE")
    {
        transaccionId ??= "tx-" + Guid.NewGuid().ToString("N")[..12];
        var centavos = (long)(valorEnPesos * 100);
        var checksum = FirmaWompi.ChecksumDeEvento([transaccionId, estado, centavos.ToString()], "1530291411", SecretoDeEventos);
        var cuerpo = "{\"event\":\"transaction.updated\",\"data\":{\"transaction\":{\"id\":\"" + transaccionId
            + "\",\"status\":\"" + estado + "\",\"amount_in_cents\":" + centavos
            + ",\"reference\":\"" + referencia + "\",\"payment_method_type\":\"" + medio + "\"}},"
            + "\"signature\":{\"properties\":[\"transaction.id\",\"transaction.status\",\"transaction.amount_in_cents\"],"
            + "\"checksum\":\"" + checksum + "\"},\"timestamp\":1530291411}";
        return (cuerpo, checksum);
    }

    /// <summary>Envía una notificación a la API como lo haría la pasarela, sin sesión.</summary>
    public async Task<HttpResponseMessage> EnviarNotificacionAsync(
        string cuerpo, string? checksum, HttpClient? http = null)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/plataforma/pagos/notificaciones")
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
        };
        if (checksum is not null)
        {
            peticion.Headers.Add("X-Event-Checksum", checksum);
        }

        return await (http ?? _fabrica.CreateClient()).SendAsync(peticion);
    }

    /// <summary>Hash corto de un texto, para comparar sin guardar valores.</summary>
    public static string Hash(string texto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
}
