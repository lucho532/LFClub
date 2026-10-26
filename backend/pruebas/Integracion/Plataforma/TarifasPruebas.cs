using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Los rangos de la tarifa del cobro de la plataforma y el mes de inicio del cobro (spec 017, RF-001, RF-002 y RF-005a).</summary>
[Collection(ColeccionApi.Nombre)]
public class TarifasPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public TarifasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose() => _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();

    private static object[] Rangos(params (int desde, int? hasta, decimal precio)[] rangos) =>
        rangos.Select(r => (object)new { desde = r.desde, hasta = r.hasta, precio = r.precio }).ToArray();

    [Fact]
    public async Task El_desarrollador_guarda_y_lee_los_rangos_de_la_tarifa()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var guardado = await cliente.PutAsync(
            "/api/plataforma/tarifas",
            new { rangos = Rangos((1, 20, 25000), (21, 40, 45000), (41, null, 75000)), diasDeGracia = 7 });

        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);
        var leido = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/plataforma/tarifas"));
        var rangos = leido.GetProperty("rangos").EnumerateArray().ToList();
        Assert.Equal(3, rangos.Count);
        Assert.Equal(
            (21, 40, 45000),
            (rangos[1].GetProperty("desde").GetInt32(), rangos[1].GetProperty("hasta").GetInt32(), rangos[1].GetProperty("precio").GetInt32()));
        Assert.Equal(JsonValueKind.Null, rangos[2].GetProperty("hasta").ValueKind);
        Assert.Equal(7, leido.GetProperty("diasDeGracia").GetInt32());
        Assert.True(leido.GetProperty("pasarelaConfigurada").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, leido.GetProperty("actualizadoEn").ValueKind);
    }

    [Fact]
    public async Task Guardar_sin_rangos_deja_la_plataforma_sin_cobrar()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync("/api/plataforma/tarifas", new { rangos = Array.Empty<object>(), diasDeGracia = 5 });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("rangos").EnumerateArray());
    }

    [Theory]
    [InlineData(0, 20, 21, null, 25000, "El primer rango empieza en 1")]
    [InlineData(1, 20, 22, null, 25000, "debe empezar en 21")]
    [InlineData(1, null, 21, null, 25000, "debe terminar")]
    [InlineData(1, 20, 21, 40, 25000, "no tiene l")]
    [InlineData(1, 20, 21, null, -1, "no puede ser negativo")]
    public async Task Unos_rangos_mal_armados_responden_400_con_el_motivo(int desde1, int? hasta1, int desde2, int? hasta2, int precio1, string motivo)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync(
            "/api/plataforma/tarifas",
            new { rangos = Rangos((desde1, hasta1, precio1), (desde2, hasta2, 45000)), diasDeGracia = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Contains(motivo, await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(31)]
    [InlineData(-1)]
    public async Task Unos_dias_de_gracia_fuera_de_rango_responden_400(int dias)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync("/api/plataforma/tarifas", new { rangos = Rangos((1, null, 1500)), diasDeGracia = dias });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Un_precio_con_centavos_o_demasiados_rangos_responden_400()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var muchos = Enumerable.Range(0, 11)
            .Select(i => (desde: (i * 10) + 1, hasta: i == 10 ? (int?)null : (i + 1) * 10, precio: 1000m))
            .ToArray();

        var centavos = await cliente.PutAsync("/api/plataforma/tarifas", new { rangos = Rangos((1, null, 1500.5m)), diasDeGracia = 5 });
        var demasiados = await cliente.PutAsync("/api/plataforma/tarifas", new { rangos = Rangos(muchos), diasDeGracia = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, centavos.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, demasiados.StatusCode);
    }

    [Fact]
    public async Task La_lista_por_club_trae_el_rango_y_el_valor_de_cada_club_sin_datos_personales()
    {
        await _fabrica.CobrosPlataforma.FijarRangosAsync([new(1, 2, 10000), new(3, null, 30000)]);
        var chico = await _fabrica.Sembrador.CrearClubAsync();
        var (_, presidente) = await _fabrica.Sembrador.CrearIntegranteAsync(chico, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(chico, Rol.JUGADOR);
        var grande = await _fabrica.Sembrador.CrearClubAsync();
        for (var i = 0; i < 3; i++)
        {
            await _fabrica.Sembrador.CrearIntegranteAsync(grande, Rol.JUGADOR);
        }

        var vacio = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.GetAsync("/api/plataforma/tarifas/clubes");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var clubes = JsonSerializer.Deserialize<JsonElement>(texto).EnumerateArray().ToList();
        JsonElement De(Guid id) => clubes.Single(c => c.GetProperty("clubId").GetGuid() == id);
        Assert.Equal((1, 10000), (De(chico.Id).GetProperty("jugadores").GetInt32(), De(chico.Id).GetProperty("valorMensual").GetInt32()));
        Assert.Equal((3, 30000), (De(grande.Id).GetProperty("jugadores").GetInt32(), De(grande.Id).GetProperty("valorMensual").GetInt32()));
        Assert.Equal(JsonValueKind.Null, De(grande.Id).GetProperty("rango").GetProperty("hasta").ValueKind);
        Assert.Equal(JsonValueKind.Null, De(vacio.Id).GetProperty("rango").ValueKind);
        Assert.Equal(0, De(vacio.Id).GetProperty("valorMensual").GetInt32());
        Assert.DoesNotContain(presidente.Nombres, texto);
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
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/plataforma/tarifas/clubes")).StatusCode);
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await cliente.PutAsync("/api/plataforma/tarifas", new { rangos = Array.Empty<object>(), diasDeGracia = 5 })).StatusCode);
        }
    }

    [Fact]
    public async Task Sin_sesion_las_tarifas_responden_401()
    {
        var cliente = _fabrica.CrearClienteDePrueba();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/plataforma/tarifas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/plataforma/tarifas/clubes")).StatusCode);
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
