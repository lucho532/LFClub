using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Un jugador aprobado desde la sala de espera queda en la categoría de su año, o sin categoría si
/// el club no la tiene activa, y entra en ella al crearse o reactivarse (constitución §12.1.1 y
/// §20; RF-008 a RF-013 de la 003). Aprueba el PRESIDENTE y sin elegir rol. La ubicación de quien
/// entra con una invitación está en <c>RegistroDirectoUbicacionPruebas</c>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class UbicacionAutomaticaPruebas
{
    private readonly FabricaApi _fabrica;

    public UbicacionAutomaticaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Sin_categoria_trae_solo_a_los_jugadores_aprobados_y_activos_que_no_tienen_y_sin_datos_de_contacto()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var (_, zeta) = await e.Sembrar.CrearJugadorAsync(e.Club, 2017, apellidos: "Zapata");
        var (_, alba) = await e.Sembrar.CrearJugadorAsync(e.Club, 2018, apellidos: "Álvarez");
        await e.Sembrar.CrearJugadorAsync(e.Club, 2016, categoria);
        await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2017);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2017);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);

        foreach (var cliente in new[] { e.Presidente, e.Directivo })
        {
            var respuesta = await cliente.GetAsync(e.SinCategoria);
            var lista = (await respuesta.JsonAsync()).EnumerateArray().ToList();

            // Ni en espera, ni retirados, ni entrenador, directivo o presidente (RF-012); por apellidos.
            Assert.Equal([alba.Id, e.IntegranteJugador.Id, zeta.Id], lista.Ids("usuarioRolId"));
            Assert.All(lista, jugador => Assert.False(jugador.GetProperty("fueraDeSuAnio").GetBoolean()));
            Assert.Equal(2018, lista[0].GetProperty("anioNacimiento").GetInt32());
            await ComprobarQueNoLlevaDatosDeContactoAsync(respuesta, alba);
        }
    }

    [Fact]
    public async Task El_detalle_trae_a_los_jugadores_de_la_categoria_y_a_nadie_mas()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var (_, deSuAnio) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016, categoria, apellidos: "Castro");
        var (_, mayor) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria, apellidos: "Arango");
        await e.Sembrar.CrearJugadorAsync(e.Club, 2015, otra);

        var respuesta = await e.Directivo.GetAsync(e.RutaCategoria(categoria.Id));
        var detalle = await respuesta.JsonAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2, detalle.GetProperty("numeroJugadores").GetInt32());
        var jugadores = detalle.Lista("jugadores");
        Assert.Equal([mayor.Id, deSuAnio.Id], jugadores.Ids("usuarioRolId"));
        Assert.True(jugadores[0].GetProperty("fueraDeSuAnio").GetBoolean());
        Assert.Equal(2015, jugadores[0].GetProperty("anioNacimiento").GetInt32());
        Assert.False(jugadores[1].GetProperty("fueraDeSuAnio").GetBoolean());
        Assert.Empty(jugadores[1].Lista("equipos"));
        await ComprobarQueNoLlevaDatosDeContactoAsync(respuesta, mayor);
    }

    [Fact]
    public async Task Un_entrenador_y_un_jugador_no_ven_sin_categoria()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);

        foreach (var cliente in new[] { e.Entrenador, e.Jugador })
        {
            var respuesta = await cliente.GetAsync(e.SinCategoria);

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(e.IntegranteJugador.Apellidos, await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Aprobar_sin_elegir_rol_lo_deja_como_jugador_en_la_categoria_activa_de_su_anio()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2016);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2016);

        var respuesta = await e.Presidente.PostAsync(Aprobacion(e, enEspera.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(categoriaId, (await e.IntegranteGuardadoAsync(enEspera.Id))!.CategoriaId);
        Assert.Contains(enEspera.Id, (await e.DetalleAsync(categoriaId)).Lista("jugadores").Ids("usuarioRolId"));
        Assert.DoesNotContain(enEspera.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.False((await e.DetalleAsync(categoriaId)).GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sin_la_categoria_activa_de_su_anio_el_ingreso_se_aprueba_igual_y_queda_sin_categoria(bool existeInactiva)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.CrearCategoriaPorApiAsync(2015);
        if (existeInactiva)
        {
            await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false);
        }

        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2016);

        var respuesta = await e.Presidente.PostAsync(Aprobacion(e, enEspera.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = (await e.IntegranteGuardadoAsync(enEspera.Id))!;
        Assert.Equal(EstadoIngreso.APROBADO, guardado.EstadoIngreso);
        Assert.Equal(Rol.JUGADOR, guardado.Rol);
        Assert.Null(guardado.CategoriaId);
        Assert.Contains(enEspera.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Crear_o_reactivar_recoge_a_todos_los_sin_categoria_de_ese_anio_y_a_ninguno_mas(bool reactivando)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2012);
        var (_, deEseAnio) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016);
        var (_, tambien) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016);
        var (_, deOtroAnio) = await e.Sembrar.CrearJugadorAsync(e.Club, 2017);
        var (_, yaUbicado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016, otra);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2016);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);

        var respuesta = reactivando
            ? await e.Presidente.PostAsync(e.Reactivacion((await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false)).Id))
            : await e.Presidente.PostAsync(e.Categorias, new { anio = 2016 });
        var resultado = await respuesta.JsonAsync();
        var categoriaId = resultado.GetProperty("categoria").GetProperty("categoriaId").GetGuid();

        Assert.Equal(2, resultado.GetProperty("jugadoresUbicados").GetInt32());
        var jugadores = (await e.DetalleAsync(categoriaId)).Lista("jugadores").Ids("usuarioRolId");
        Assert.Equal(new[] { deEseAnio.Id, tambien.Id }.Order(), jugadores.Order());

        // RF-011: no mueve a quien ya tiene categoría; tampoco a otro año, en espera ni retirado.
        Assert.Equal(otra.Id, (await e.IntegranteGuardadoAsync(yaUbicado.Id))!.CategoriaId);
        Assert.Null((await e.IntegranteGuardadoAsync(deOtroAnio.Id))!.CategoriaId);
        Assert.Null((await e.IntegranteGuardadoAsync(enEspera.Id))!.CategoriaId);
        Assert.Null((await e.IntegranteGuardadoAsync(retirado.Id))!.CategoriaId);
        Assert.Empty((await e.IntegranteGuardadoAsync(deEseAnio.Id))!.Equipos);
    }

    [Fact]
    public async Task Aprobar_a_un_jugador_mientras_se_crea_la_categoria_de_su_anio_lo_deja_siempre_en_ella()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);

        foreach (var anio in Enumerable.Range(2000, 6))
        {
            var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, anio);

            var respuestas = await Task.WhenAll(
                e.Presidente.PostAsync(Aprobacion(e, enEspera.Id)),
                e.Presidente.PostAsync(e.Categorias, new { anio }));

            Assert.All(respuestas, respuesta => Assert.True(respuesta.IsSuccessStatusCode, $"{anio}: {(int)respuesta.StatusCode}"));
            var categoriaId = (await respuestas[1].JsonAsync()).GetProperty("categoria").GetProperty("categoriaId").GetGuid();
            Assert.Equal(categoriaId, (await e.IntegranteGuardadoAsync(enEspera.Id))!.CategoriaId);
        }
    }

    private static string Aprobacion(EscenarioCategorias e, Guid usuarioRolId) =>
        $"/api/clubes/{e.Club.Id}/ingresos/{usuarioRolId}/aprobacion";

    /// <summary>Ninguna lista de jugadores devuelve documento, correo ni celular (RF-032).</summary>
    private async Task ComprobarQueNoLlevaDatosDeContactoAsync(HttpResponseMessage respuesta, Dominio.Entidades.UsuarioRol jugador)
    {
        // Desde la 005 las listas llevan cuántos documentos de la ficha faltan (RF-032 de la 005). Ese
        // contador no es un dato de contacto: se aparta su nombre para que "documento" siga vigilando el
        // documento de identidad.
        var json = (await respuesta.Content.ReadAsStringAsync())
            .Replace("documentosPendientes", string.Empty, StringComparison.Ordinal);
        var correo = await _fabrica.ConContextoAsync(contexto => Task.FromResult(
            contexto.Usuarios.Single(usuario => usuario.Id == jugador.UsuarioId).Correo));

        Assert.DoesNotContain(jugador.NumeroDocumento, json);
        Assert.DoesNotContain(correo, json);
        Assert.DoesNotContain("3001234567", json);
        foreach (var campo in new[] { "documento", "correo", "celular", "fechaNacimiento", "responsable" })
        {
            Assert.DoesNotContain(campo, json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
