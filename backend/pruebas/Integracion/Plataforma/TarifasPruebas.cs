using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Las tarifas del cobro de la plataforma y el mes de inicio del cobro (spec 017, RF-001, RF-002 y RF-005a).</summary>
[Collection(ColeccionApi.Nombre)]
public class TarifasPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public TarifasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose() => _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();

    [Fact]
    public async Task El_desarrollador_guarda_y_lee_las_tarifas()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var guardado = await cliente.PutAsync(
            "/api/plataforma/tarifas",
            new { precioJugador = 1500, precioEntrenador = (int?)null, precioDirectivo = 3000, precioPresidente = 0, diasDeGracia = 7 });

        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);
        var leido = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/plataforma/tarifas"));
        Assert.Equal(1500, leido.GetProperty("precioJugador").GetDecimal());
        Assert.Equal(JsonValueKind.Null, leido.GetProperty("precioEntrenador").ValueKind);
        Assert.Equal(3000, leido.GetProperty("precioDirectivo").GetDecimal());
        Assert.Equal(0, leido.GetProperty("precioPresidente").GetDecimal());
        Assert.Equal(7, leido.GetProperty("diasDeGracia").GetInt32());
        Assert.True(leido.GetProperty("pasarelaConfigurada").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, leido.GetProperty("actualizadoEn").ValueKind);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(1500, 31)]
    [InlineData(1500, -1)]
    public async Task Un_precio_negativo_o_unos_dias_de_gracia_fuera_de_rango_responden_400(int precio, int dias)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync(
            "/api/plataforma/tarifas", new { precioJugador = precio, diasDeGracia = dias });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Un_precio_con_centavos_responde_400()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync("/api/plataforma/tarifas", new { precioJugador = 1500.5, diasDeGracia = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Las_tarifas_no_las_ven_ni_las_cambian_el_presidente_ni_los_demas_roles()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        foreach (var rol in new[] { Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR, Rol.JUGADOR })
        {
            var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
            var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/plataforma/tarifas")).StatusCode);
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await cliente.PutAsync("/api/plataforma/tarifas", new { precioJugador = 1, diasDeGracia = 5 })).StatusCode);
        }
    }

    [Fact]
    public async Task Sin_sesion_las_tarifas_responden_401()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/plataforma/tarifas")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await cliente.PutAsync("/api/plataforma/tarifas", new { diasDeGracia = 5 })).StatusCode);
    }

    [Fact]
    public async Task La_respuesta_de_las_tarifas_no_trae_llaves_ni_secretos()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var texto = await (await cliente.GetAsync("/api/plataforma/tarifas")).Content.ReadAsStringAsync();

        Assert.DoesNotContain(SembradorCobrosPlataforma.LlavePublica, texto);
        Assert.DoesNotContain(SembradorCobrosPlataforma.SecretoDeIntegridad, texto);
        Assert.DoesNotContain(SembradorCobrosPlataforma.SecretoDeEventos, texto);
    }

    [Fact]
    public async Task El_desarrollador_fija_y_borra_el_mes_de_inicio_del_cobro_de_un_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var fijado = await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/cobro", new { mesDeInicio = "2026-12" });

        Assert.Equal(HttpStatusCode.OK, fijado.StatusCode);
        var estado = await ClienteDePrueba.LeerAsync<JsonElement>(fijado);
        Assert.Equal("2026-12", estado.GetProperty("mesDeInicioDelCobro").GetString());
        Assert.Equal(new DateOnly(2026, 12, 1), (await _fabrica.CobrosPlataforma.ClubAsync(club)).MesInicioCobro);

        var borrado = await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/cobro", new { mesDeInicio = (string?)null });

        Assert.Equal(HttpStatusCode.OK, borrado.StatusCode);
        Assert.Equal("SIN_COBRO", (await ClienteDePrueba.LeerAsync<JsonElement>(borrado)).GetProperty("estadoDePago").GetString());
        Assert.Null((await _fabrica.CobrosPlataforma.ClubAsync(club)).MesInicioCobro);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("diciembre")]
    [InlineData("2026-12-01")]
    public async Task Un_mes_con_formato_invalido_responde_400(string mes)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/cobro", new { mesDeInicio = mes });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Null((await _fabrica.CobrosPlataforma.ClubAsync(club)).MesInicioCobro);
    }

    [Fact]
    public async Task Fijar_el_mes_de_inicio_de_un_club_inexistente_responde_404()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync($"/api/plataforma/clubes/{Guid.NewGuid()}/cobro", new { mesDeInicio = "2026-12" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
