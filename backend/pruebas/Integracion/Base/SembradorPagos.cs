using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Arma clubes con jugadores, cargos y pagos directamente en la base de datos, para las pruebas de la
/// funcionalidad 015. Las pruebas que construyen un dato por la API lo hacen ellas mismas.
/// </summary>
public class SembradorPagos
{
    private readonly FabricaApi _fabrica;

    public SembradorPagos(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Club con valor de mensualidad, presidente, directivo, entrenador de la 2014, y dos jugadores de
    /// la 2014 de familias distintas (Ana y Beto); ambos cobran desde el mes pasado.
    /// </summary>
    public async Task<ClubDePagos> CrearClubAsync(decimal? valor = 65000m)
    {
        var sembrador = _fabrica.Sembrador;
        var categorias = _fabrica.Categorias;
        var club = await sembrador.CrearClubAsync();
        var presidente = await sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var directivo = await sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var entrenador = await sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var categoria = await categorias.CrearCategoriaAsync(club, 2014);
        await categorias.AsignarEntrenadorAsync(categoria, entrenador.Integrante);
        var ana = await categorias.CrearJugadorAsync(club, 2014, categoria, "Ana", $"Arango {Sembrador.Unico()}");
        var beto = await categorias.CrearJugadorAsync(club, 2014, categoria, "Beto", $"Bermúdez {Sembrador.Unico()}");

        await FijarValorAsync(club, valor);
        await FijarCobraDesdeAsync(ana.Integrante, PrimerDiaDelMes(-1));
        await FijarCobraDesdeAsync(beto.Integrante, PrimerDiaDelMes(-1));
        return new ClubDePagos(club, presidente, directivo, entrenador, categoria, ana, beto);
    }

    /// <summary>Primer día del mes de Colombia, desplazado esa cantidad de meses, según el reloj de la API.</summary>
    public DateOnly PrimerDiaDelMes(int desplazamiento = 0)
    {
        var hoy = _fabrica.Reloj.Hoy;
        return new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(desplazamiento);
    }

    public Task FijarValorAsync(Club club, decimal? valor) => _fabrica.ConContextoAsync(contexto =>
        contexto.Clubes.Where(fila => fila.Id == club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.ValorMensualidad, valor)));

    public Task FijarCobraDesdeAsync(UsuarioRol jugador, DateOnly? mes) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.CobraDesde, mes)));

    public Task FijarValorPropioAsync(UsuarioRol jugador, decimal? valor) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.ValorMensualidadPropio, valor)));

    public async Task<Cargo> CrearCargoAsync(UsuarioRol jugador, DateOnly mes, decimal valor = 65000m)
    {
        var cargo = new Cargo
        {
            ClubId = jugador.ClubId,
            UsuarioRolId = jugador.Id,
            Tipo = TipoCargo.MENSUALIDAD,
            Mes = mes,
            Valor = valor,
            CategoriaId = jugador.CategoriaId,
            CreadoEn = DateTime.UtcNow,
        };

        return await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.Cargos.Add(cargo);
            await contexto.SaveChangesAsync();
            return cargo;
        });
    }

    /// <summary>Pago confirmado que cubre esos cargos, en ese orden, hasta agotar su valor.</summary>
    public async Task<Pago> CrearPagoConfirmadoAsync(
        UsuarioRol jugador, UsuarioRol presidente, decimal valor, params Cargo[] cargos)
    {
        var pago = NuevoPago(jugador, valor, EstadoPago.CONFIRMADO);
        pago.CreadoPorUsuarioId = presidente.UsuarioId;
        pago.CreadoPorNombre = $"{presidente.Nombres} {presidente.Apellidos}";
        pago.ConfirmadoEn = DateTime.UtcNow;
        pago.ConfirmadoPorUsuarioId = presidente.UsuarioId;
        pago.ConfirmadoPorNombre = pago.CreadoPorNombre;
        pago.SaldoTrasPago = 0;

        var restante = valor;
        foreach (var cargo in cargos)
        {
            var parte = Math.Min(restante, cargo.Valor);
            if (parte <= 0)
            {
                break;
            }

            pago.Aplicaciones.Add(new AplicacionPago { CargoId = cargo.Id, ClubId = jugador.ClubId, Valor = parte });
            restante -= parte;
        }

        return await GuardarAsync(pago);
    }

    /// <summary>Cambia el instante en que se confirmó un pago, para ponerlo en otro mes.</summary>
    public Task FijarConfirmadoEnAsync(Pago pago, DateTime confirmadoEnUtc) => _fabrica.ConContextoAsync(contexto =>
        contexto.Pagos.IgnoreQueryFilters().Where(fila => fila.Id == pago.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.ConfirmadoEn, confirmadoEnUtc)));

    /// <summary>Cambia la categoría actual de un jugador, sin tocar sus cargos ya generados.</summary>
    public Task CambiarCategoriaAsync(UsuarioRol jugador, Guid? categoriaId) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.Id == jugador.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.CategoriaId, categoriaId)));

    /// <summary>Marca un pago confirmado como anulado, con sus datos de anulación.</summary>
    public Task AnularAsync(Pago pago, UsuarioRol presidente) => _fabrica.ConContextoAsync(contexto =>
        contexto.Pagos.IgnoreQueryFilters().Where(fila => fila.Id == pago.Id).ExecuteUpdateAsync(cambios => cambios
            .SetProperty(fila => fila.Estado, EstadoPago.ANULADO)
            .SetProperty(fila => fila.AnuladoEn, DateTime.UtcNow)
            .SetProperty(fila => fila.AnuladoPorUsuarioId, presidente.UsuarioId)
            .SetProperty(fila => fila.AnuladoPorNombre, $"{presidente.Nombres} {presidente.Apellidos}")
            .SetProperty(fila => fila.MotivoAnulacion, "Se equivocó")));

    public async Task<Pago> CrearPagoEnRevisionAsync(
        Club club, UsuarioRol jugador, decimal valor, bool conSoporte = false)
    {
        var pago = NuevoPago(jugador, valor, EstadoPago.EN_REVISION);
        pago.Medio = MedioPago.TRANSFERENCIA;
        pago.Entidad = "Nequi";
        pago.CreadoPorUsuarioId = jugador.UsuarioId;
        pago.CreadoPorNombre = pago.JugadorNombre;
        await GuardarAsync(pago);

        if (conSoporte)
        {
            await _fabrica.ConContextoAsync(async contexto =>
            {
                contexto.SoportesPago.Add(new SoportePago
                {
                    PagoId = pago.Id,
                    ClubId = club.Id,
                    Contenido = Imagenes.Png(),
                    TipoContenido = "image/png",
                    GuardadaEn = DateTime.UtcNow,
                });
                await contexto.SaveChangesAsync();
                return 0;
            });
        }

        return pago;
    }

    /// <summary>
    /// Siembra de una vez lo que un club puede tener de pagos: un cargo, un pago confirmado con su
    /// aplicación, otro en revisión con su soporte y un medio de pago.
    /// </summary>
    public async Task SembrarCuentaCompletaAsync(Club club, UsuarioRol jugador, UsuarioRol presidente)
    {
        var cargo = await CrearCargoAsync(jugador, new DateOnly(2026, 9, 1));
        await CrearPagoConfirmadoAsync(jugador, presidente, 30000m, cargo);
        await CrearPagoEnRevisionAsync(club, jugador, 20000m, conSoporte: true);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.MediosPagoClub.Add(new MedioPagoClub
            {
                ClubId = club.Id, Entidad = "Nequi", Titular = "Club", Numero = "3001234567", CreadoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });
    }

    /// <summary>Dispara la generación mensual de todos los clubes, como lo hace el trabajo en segundo plano.</summary>
    public async Task<int> GenerarAsync()
    {
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioGeneracionCargos>().GenerarAsync();
    }

    /// <summary>Los cargos de un club, de cualquier jugador, ordenados por mes.</summary>
    public Task<List<Cargo>> CargosDelClubAsync(Club club) => _fabrica.ConContextoAsync(contexto =>
        contexto.Cargos.IgnoreQueryFilters().Where(cargo => cargo.ClubId == club.Id)
            .OrderBy(cargo => cargo.Mes).ThenBy(cargo => cargo.UsuarioRolId).ToListAsync());

    /// <summary>Número de aplicaciones y suma de lo aplicado a un cargo por pagos confirmados.</summary>
    public Task<decimal> PagadoAsync(Cargo cargo) => _fabrica.ConContextoAsync(async contexto =>
        (await contexto.AplicacionesPago.IgnoreQueryFilters()
            .Where(aplicacion => aplicacion.CargoId == cargo.Id
                && contexto.Pagos.IgnoreQueryFilters()
                    .Any(pago => pago.Id == aplicacion.PagoId && pago.Estado == EstadoPago.CONFIRMADO))
            .Select(aplicacion => (decimal?)aplicacion.Valor)
            .SumAsync()) ?? 0m);

    private static Pago NuevoPago(UsuarioRol jugador, decimal valor, EstadoPago estado) => new()
    {
        ClubId = jugador.ClubId,
        UsuarioRolId = jugador.Id,
        JugadorNombre = $"{jugador.Nombres} {jugador.Apellidos}",
        JugadorTipoDocumento = jugador.TipoDocumento.ToString(),
        JugadorNumeroDocumento = jugador.NumeroDocumento,
        Medio = MedioPago.EFECTIVO,
        Fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)),
        Valor = valor,
        Estado = estado,
        CreadoEn = DateTime.UtcNow,
        NumeroRecibo = estado == EstadoPago.CONFIRMADO ? Interlocked.Increment(ref _recibo) : null,
    };

    private static int _recibo = 1_000_000;

    private async Task<Pago> GuardarAsync(Pago pago) => await _fabrica.ConContextoAsync(async contexto =>
    {
        contexto.Pagos.Add(pago);
        await contexto.SaveChangesAsync();
        return pago;
    });
}

/// <summary>Club de las pruebas de pagos con todo lo que crea el sembrador.</summary>
public record ClubDePagos(
    Club Club,
    (Usuario Usuario, UsuarioRol Integrante) Presidente,
    (Usuario Usuario, UsuarioRol Integrante) Directivo,
    (Usuario Usuario, UsuarioRol Integrante) Entrenador,
    Categoria Categoria,
    (Usuario Usuario, UsuarioRol Integrante) Ana,
    (Usuario Usuario, UsuarioRol Integrante) Beto)
{
    /// <summary>Ruta de la API del club, con lo que siga.</summary>
    public string Ruta(string resto = "") => $"/api/clubes/{Club.Id}{resto}";

    /// <summary>Ruta de la cuenta de un jugador.</summary>
    public string Cuenta(UsuarioRol jugador) => Ruta($"/jugadores/{jugador.Id}/cuenta");
}
