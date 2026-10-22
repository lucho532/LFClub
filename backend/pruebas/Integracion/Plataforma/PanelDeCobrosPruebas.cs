using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>El panel del DESARROLLADOR sobre los cobros: el estado de pago de los clubes, las notificaciones de la pasarela y quién puede verlo (spec 017, historia 5).</summary>
[Collection(ColeccionApi.Nombre)]
public class PanelDeCobrosPruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public PanelDeCobrosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static string Ruta(Club club) => $"/api/plataforma/clubes/{club.Id}/cobros";

    [Fact]
    public async Task La_lista_de_clubes_trae_el_estado_de_pago_y_el_valor_adeudado_de_cada_club()
    {
        var alDia = await _fabrica.Sembrador.CrearClubAsync();
        var pendiente = await _fabrica.Sembrador.CrearClubAsync();
        var vencido = await _fabrica.Sembrador.CrearClubAsync();
        var sinCobro = await _fabrica.Sembrador.CrearClubAsync();
        var hoy = new DateOnly(2026, 11, 3);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(alDia, Noviembre);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(pendiente, Noviembre);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(vencido, Noviembre);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(alDia, Noviembre, hoy.AddDays(3), estado: EstadoCobroPlataforma.PAGADO);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(pendiente, Noviembre, hoy.AddDays(3), 45000);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(vencido, Noviembre, hoy.AddDays(-1), 30000);
        _fabrica.Reloj.UsarElReal();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        _fabrica.Reloj.FijarEnColombia(hoy, new TimeOnly(10, 0));

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync("/api/plataforma/clubes"));

        JsonElement Fila(Club club) => lista.EnumerateArray().Single(c => c.GetProperty("clubId").GetGuid() == club.Id);
        Assert.Equal("AL_DIA", Fila(alDia).GetProperty("estadoDePago").GetString());
        Assert.Equal("PENDIENTE", Fila(pendiente).GetProperty("estadoDePago").GetString());
        Assert.Equal(45000, Fila(pendiente).GetProperty("valorAdeudado").GetDecimal());
        Assert.Equal("VENCIDO", Fila(vencido).GetProperty("estadoDePago").GetString());
        Assert.Equal("SIN_COBRO", Fila(sinCobro).GetProperty("estadoDePago").GetString());
        Assert.Equal("2026-11", Fila(pendiente).GetProperty("mesDeInicioDelCobro").GetString());
    }

    [Fact]
    public async Task El_detalle_de_un_club_suspendido_por_impago_trae_el_motivo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5), 30000);
        _fabrica.Reloj.UsarElReal();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 20), new TimeOnly(10, 0));
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync($"/api/plataforma/clubes/{club.Id}"));

        Assert.Equal("SUSPENDIDO_POR_IMPAGO", detalle.GetProperty("estadoDePago").GetString());
        Assert.Equal("IMPAGO", detalle.GetProperty("motivoDeSuspension").GetString());
        Assert.Equal(30000, detalle.GetProperty("valorAdeudado").GetDecimal());
    }

    [Fact]
    public async Task Las_notificaciones_listan_duplicados_y_se_filtran_por_resultado()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5), 45000);
        var uno = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        var dos = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        foreach (var (referencia, transaccion) in new[] { (uno.Referencia, "tx-filtro-1"), (dos.Referencia, "tx-filtro-2") })
        {
            var (cuerpo, checksum) = Notificacion(referencia, 45000, transaccionId: transaccion);
            await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);
        }

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var todas = await ClienteDePrueba.LeerAsync<JsonElement>(await desarrollador.GetAsync("/api/plataforma/notificaciones"));
        var duplicados = await ClienteDePrueba.LeerAsync<JsonElement>(
            await desarrollador.GetAsync("/api/plataforma/notificaciones?resultado=PAGO_DUPLICADO"));

        Assert.True(todas.GetArrayLength() <= 100);
        var duplicado = duplicados.EnumerateArray().Single(n => n.GetProperty("transaccionId").GetString() == "tx-filtro-2");
        Assert.Equal(club.Id, duplicado.GetProperty("clubId").GetGuid());
        Assert.Equal("2026-11", duplicado.GetProperty("periodo").GetString());
        Assert.All(duplicados.EnumerateArray(), n => Assert.Equal("PAGO_DUPLICADO", n.GetProperty("resultado").GetString()));
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await desarrollador.GetAsync("/api/plataforma/notificaciones?resultado=INVENTADO")).StatusCode);
    }

    [Fact]
    public async Task Todo_se_rechaza_al_presidente_y_a_los_demas_roles_y_a_quien_no_tiene_sesion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        var rutas = new (string Metodo, string Ruta)[]
        {
            ("GET", Ruta(club)),
            ("POST", $"{Ruta(club)}/{cobro.Id}/pago-manual"),
            ("POST", $"{Ruta(club)}/{cobro.Id}/anulacion"),
            ("GET", "/api/plataforma/notificaciones"),
        };

        var anonimo = _fabrica.CrearClienteDePrueba();
        foreach (var (metodo, ruta) in rutas)
        {
            var respuesta = metodo == "GET" ? await anonimo.GetAsync(ruta) : await anonimo.PostAsync(ruta, new { motivo = "Prueba" });
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        foreach (var rol in new[] { Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR })
        {
            var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
            var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
            foreach (var (metodo, ruta) in rutas)
            {
                var respuesta = metodo == "GET" ? await cliente.GetAsync(ruta) : await cliente.PostAsync(ruta, new { motivo = "Prueba" });
                Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            }
        }

        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await _fabrica.CobrosPlataforma.CobrosDeAsync(club)).Single().Estado);
    }
}
