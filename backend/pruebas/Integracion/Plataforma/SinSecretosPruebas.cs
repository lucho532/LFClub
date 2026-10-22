using System.Net;
using System.Reflection;
using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// Ninguna llave ni secreto de la pasarela sale de la API: ni en los DTO, ni en las respuestas, ni en
/// el registro de eventos (constitución §7.4, spec 017, RF-025 y CE-008).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SinSecretosPruebas
{
    private readonly FabricaApi _fabrica;

    public SinSecretosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public void Ningun_dto_tiene_propiedades_de_llave_secreto_o_checksum()
    {
        var prohibidas = typeof(TarifasDto).Assembly.GetTypes()
            .Where(tipo => tipo.Namespace == typeof(TarifasDto).Namespace)
            .SelectMany(tipo => tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(propiedad => propiedad.Name.StartsWith("Llave", StringComparison.OrdinalIgnoreCase)
                    || propiedad.Name.StartsWith("Secreto", StringComparison.OrdinalIgnoreCase)
                    || propiedad.Name.StartsWith("Checksum", StringComparison.OrdinalIgnoreCase))
                .Select(propiedad => $"{tipo.Name}.{propiedad.Name}"))
            .ToList();

        Assert.Empty(prohibidas);
    }

    [Fact]
    public async Task Ninguna_respuesta_de_esta_funcionalidad_contiene_los_secretos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var ruta = $"/api/clubes/{club.Id}/pago-plataforma";

        var respuestas = new List<HttpResponseMessage>
        {
            await presidente.GetAsync(ruta),
            await presidente.GetAsync($"{ruta}/cobros/{cobro.Id}"),
            await presidente.PostAsync($"{ruta}/cobros/{cobro.Id}/intentos"),
            await desarrollador.GetAsync("/api/plataforma/tarifas"),
            await desarrollador.GetAsync("/api/plataforma/clubes"),
            await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}"),
            await desarrollador.GetAsync("/api/plataforma/notificaciones"),
            await _fabrica.CobrosPlataforma.EnviarNotificacionAsync("{}", "x"),
        };

        foreach (var respuesta in respuestas)
        {
            var texto = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(SecretoDeIntegridad, texto);
            Assert.DoesNotContain(SecretoDeEventos, texto);
            Assert.DoesNotContain("SecretoDeEventos", texto, StringComparison.OrdinalIgnoreCase);
        }

        Assert.All(respuestas.Take(3), r => Assert.True(r.IsSuccessStatusCode));
    }

    [Fact]
    public async Task El_registro_de_eventos_de_una_notificacion_no_contiene_secretos_ni_el_cuerpo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8));
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        var (cuerpo, checksum) = Notificacion(intento.Referencia, 50000, transaccionId: "tx-registro");
        var (cuerpoMalo, _) = Notificacion(intento.Referencia, 50000, transaccionId: "tx-registro-malo");

        var buena = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);
        var mala = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpoMalo, new string('0', 64));

        Assert.Equal(HttpStatusCode.OK, buena.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mala.StatusCode);
        foreach (var mensaje in _fabrica.Registro.Mensajes)
        {
            Assert.DoesNotContain(SecretoDeIntegridad, mensaje);
            Assert.DoesNotContain(SecretoDeEventos, mensaje);
            Assert.DoesNotContain(checksum, mensaje);
            Assert.DoesNotContain("\"signature\"", mensaje);
            Assert.DoesNotContain("transaction.updated", mensaje);
        }
    }
}
