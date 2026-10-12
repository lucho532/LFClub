using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El PRESIDENTE retira del club a un jugador que se fue y lo reincorpora (constitución §14.1;
/// RF-041 a RF-047; historia 6).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RetiroJugadorPruebas
{
    private readonly FabricaApi _fabrica;

    public RetiroJugadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Tras_retirarlo_no_esta_en_su_categoria_ni_en_sus_equipos_y_aparece_en_retirados()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var (_, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var respuesta = await e.Presidente.PostAsync(e.Retiro(jugador.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var detalle = await e.DetalleAsync(categoria.Id);
        Assert.Empty(detalle.Lista("jugadores"));
        Assert.Equal(0, detalle.GetProperty("numeroJugadores").GetInt32());

        // El equipo y la categoría siguen existiendo, vacíos (caso límite).
        Assert.Equal(0, detalle.Lista("equipos").Single().GetProperty("numeroJugadores").GetInt32());
        Assert.DoesNotContain(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));

        var retirado = Assert.Single(await e.Directivo.ListaAsync(e.Retirados));
        Assert.Equal(jugador.Id, retirado.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(jugador.Nombres, retirado.GetProperty("nombres").GetString());
        Assert.Equal(jugador.Apellidos, retirado.GetProperty("apellidos").GetString());
        Assert.Equal(2015, retirado.GetProperty("anioNacimiento").GetInt32());
        Assert.Equal(
            $"{e.IntegrantePresidente.Nombres} {e.IntegrantePresidente.Apellidos}", retirado.GetProperty("retiradoPor").GetString());
        Assert.True(retirado.GetProperty("retiradoEn").GetDateTime() >= antes);

        var guardado = (await e.IntegranteGuardadoAsync(jugador.Id))!;
        Assert.False(guardado.Activo);
        Assert.Null(guardado.CategoriaId);
        Assert.Empty(guardado.Equipos);

        // Ya sin jugadores, la categoría se puede desactivar (escenario 6.8).
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(e.Desactivacion(categoria.Id))).StatusCode);

        // "Ingresos aprobados" es el registro de las aprobaciones y no cambia (supuesto 8): sigue respondiendo.
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.GetAsync($"/api/clubes/{e.Club.Id}/ingresos/aprobados")).StatusCode);
    }

    [Fact]
    public async Task Retirarlo_dos_veces_o_a_la_vez_lo_retira_una_sola_vez()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (otroUsuario, otroPresidente) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE);
        var delOtro = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(otroUsuario);
        var ruta = e.Retiro(e.IntegranteJugador.Id);

        var simultaneas = await Task.WhenAll(e.Presidente.PostAsync(ruta), delOtro.PostAsync(ruta));
        var primero = (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!;
        var repetida = await delOtro.PostAsync(ruta);
        var despues = (await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!;

        Assert.All(simultaneas, respuesta => Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode));
        Assert.Equal(HttpStatusCode.NoContent, repetida.StatusCode);
        Assert.Contains(primero.RetiradoPorUsuarioId, new Guid?[] { e.IntegrantePresidente.UsuarioId, otroPresidente.UsuarioId });
        Assert.Equal(primero.RetiradoPorUsuarioId, despues.RetiradoPorUsuarioId);
        Assert.Equal(primero.RetiradoEn, despues.RetiradoEn);
        Assert.Single(await e.Presidente.ListaAsync(e.Retirados));
    }

    [Fact]
    public async Task El_retiro_solo_aplica_a_jugadores_aprobados()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2015);
        var noJugadores = new[] { e.IntegranteEntrenador.Id, e.IntegranteDirectivo.Id, e.IntegrantePresidente.Id, enEspera.Id };

        foreach (var usuarioRolId in noJugadores)
        {
            var respuesta = await e.Presidente.PostAsync(e.Retiro(usuarioRolId));

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("no_es_jugador", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.True((await e.IntegranteGuardadoAsync(usuarioRolId))!.Activo);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await e.Presidente.PostAsync(e.Retiro(Guid.NewGuid()))).StatusCode);
        Assert.Empty(await e.Presidente.ListaAsync(e.Retirados));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reincorporarlo_lo_ubica_como_a_un_ingreso_recien_aprobado_y_no_le_devuelve_sus_equipos(bool existeLaDeSuAnio)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var deSuAnio = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015, activa: existeLaDeSuAnio);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2012);
        var equipo = await e.Sembrar.CrearEquipoAsync(otra, "A");
        var (usuario, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, otra);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        var suCliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        await e.Presidente.PostAsync(e.Retiro(jugador.Id));

        var respuesta = await e.Presidente.PostAsync(e.Reincorporacion(jugador.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await respuesta.JsonAsync();
        var guardado = (await e.IntegranteGuardadoAsync(jugador.Id))!;
        Assert.True(guardado.Activo);
        Assert.Null(guardado.RetiradoEn);
        Assert.Null(guardado.RetiradoPorUsuarioId);
        Assert.Null(guardado.RetiradoPorNombre);
        Assert.Empty(guardado.Equipos);

        // Va a la categoría activa de su año, no a la que tenía; si no existe o está inactiva, sin categoría.
        if (existeLaDeSuAnio)
        {
            Assert.Equal(deSuAnio.Id, dto.GetProperty("categoriaId").GetGuid());
            Assert.Equal(2015, dto.GetProperty("anio").GetInt32());
            Assert.Equal(deSuAnio.Id, guardado.CategoriaId);
        }
        else
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Null, dto.GetProperty("categoriaId").ValueKind);
            Assert.Equal(System.Text.Json.JsonValueKind.Null, dto.GetProperty("anio").ValueKind);
            Assert.Contains(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        }

        Assert.Empty(await e.Presidente.ListaAsync(e.Retirados));
        Assert.Equal(HttpStatusCode.OK, (await suCliente.GetAsync($"/api/clubes/{e.Club.Id}")).StatusCode);

        var otraVez = await e.Presidente.PostAsync(e.Reincorporacion(jugador.Id));
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        Assert.Equal("jugador_no_retirado", await ClienteDePrueba.CodigoAsync(otraVez));
    }

    [Fact]
    public async Task La_lista_conserva_el_nombre_de_quien_lo_retiro_aunque_esa_cuenta_ya_no_exista()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (otroUsuario, otroPresidente) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE);
        var delOtro = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(otroUsuario);
        await delOtro.PostAsync(e.Retiro(e.IntegranteJugador.Id));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        // Era su único club: al eliminarlo del club su cuenta deja de existir.
        var eliminado = await desarrollador.PostAsync(
            $"/api/plataforma/clubes/{e.Club.Id}/presidentes/{otroPresidente.Id}/retiro", new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.OK, eliminado.StatusCode);
        var retirado = Assert.Single(await e.Presidente.ListaAsync(e.Retirados));
        Assert.Equal($"{otroPresidente.Nombres} {otroPresidente.Apellidos}", retirado.GetProperty("retiradoPor").GetString());
        Assert.Null((await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.RetiradoPorUsuarioId);
    }

    // RF-042 y RF-043 de la 006: también se retira la cuenta del propio jugador, a nadie más.
    [Fact]
    public async Task Retiran_el_presidente_y_el_propio_jugador_solo_el_presidente_reincorpora_y_el_directivo_solo_ve_la_lista()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        var (_, otroJugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);

        Assert.Single(await e.Directivo.ListaAsync(e.Retirados));
        foreach (var cliente in new[] { e.Directivo, e.Entrenador })
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(cliente, HttpStatusCode.Forbidden, "rol_no_autorizado",
                ("POST", e.Retiro(e.IntegranteJugador.Id), null),
                ("POST", e.Reincorporacion(retirado.Id), null));
        }

        await AislamientoCategoriasPruebas.ComprobarAsync(
            e.Jugador, HttpStatusCode.Forbidden, "rol_no_autorizado", ("POST", e.Reincorporacion(retirado.Id), null));
        await AislamientoCategoriasPruebas.ComprobarAsync(
            e.Jugador, HttpStatusCode.NotFound, "no_encontrado", ("POST", e.Retiro(otroJugador.Id), null));

        foreach (var cliente in new[] { e.Entrenador, e.Jugador })
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(
                cliente, HttpStatusCode.Forbidden, "rol_no_autorizado", ("GET", e.Retirados, null));
        }

        var enSuClub = $"/api/clubes/{e.OtroClub.Id}/jugadores";
        await AislamientoCategoriasPruebas.ComprobarAsync(e.PresidenteDeOtroClub, HttpStatusCode.NotFound, "no_encontrado",
            ("POST", $"{enSuClub}/{e.IntegranteJugador.Id}/retiro", null),
            ("POST", $"{enSuClub}/{retirado.Id}/reincorporacion", null));
        Assert.Empty(await e.PresidenteDeOtroClub.ListaAsync($"{enSuClub}/retirados"));

        Assert.True((await e.IntegranteGuardadoAsync(otroJugador.Id))!.Activo);
        Assert.False((await e.IntegranteGuardadoAsync(retirado.Id))!.Activo);

        Assert.Equal(HttpStatusCode.NoContent, (await e.Jugador.PostAsync(e.Retiro(e.IntegranteJugador.Id))).StatusCode);
        Assert.False((await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.Activo);
    }
}
