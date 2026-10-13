using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Avisos;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Cambiar a quién va dirigido un entrenamiento: sumar jugadores de otro equipo o de otra categoría,
/// que ven y responden solo esa actividad sin cambiar de categoría ni de equipo; quién puede
/// cambiarlo; que quitar no deje aviso, respuesta ni texto; y que el detalle de quien dirige traiga
/// los equipos y los añadidos (spec 013, historias 3 y 4; RF-013 a RF-022; CE-003, CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AudienciaDeEntrenamientoPruebas : IDisposable
{
    private static readonly object Asistira = new { respuesta = "ASISTIRA" };

    private readonly FabricaApi _fabrica;

    public AudienciaDeEntrenamientoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    private static object Audiencia(IEnumerable<Equipo> equipos, params Persona[] anadidos) =>
        new { equipoIds = equipos.Select(equipo => equipo.Id), jugadorIds = anadidos.Select(jugador => jugador.Id) };

    [Fact]
    public async Task El_anadido_de_otro_equipo_y_el_de_otra_categoria_ven_y_responden_solo_esa_actividad()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var deTodos = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana.AddDays(1));
        var ruta = e.Ruta($"/entrenamientos/{deA.Id}");
        var categoriaDeK1 = e.K1.Integrante.CategoriaId;

        var cambiado = await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([a], e.Jn(5), e.K1)));

        Assert.Equal(2, cambiado.GetProperty("anadidos").GetArrayLength());
        foreach (var persona in new[] { e.Jn(5), e.K1 })
        {
            var detalle = await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta));
            Assert.True(detalle.GetProperty("permisos").GetProperty("responde").GetBoolean());
            await Actividades.LeerAsync(await persona.Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));
            Assert.False(detalle.TryGetProperty("anadidos", out _));
        }

        // No ven nada más de la 2014: ni el otro entrenamiento ni la planilla.
        Assert.Equal(HttpStatusCode.NotFound, (await e.K1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{deTodos.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await e.K1.Cliente.GetAsync($"{ruta}/jugadores")).StatusCode);

        // Su categoría y sus equipos no cambiaron.
        var jugador = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == e.K1.Id).Select(integrante => integrante.CategoriaId).SingleAsync());
        Assert.Equal(categoriaDeK1, jugador);
        var equiposDeJ5 = await _fabrica.ConContextoAsync(contexto => contexto.JugadoresEquipo.IgnoreQueryFilters()
            .Where(fila => fila.UsuarioRolId == e.Jn(5).Id).CountAsync());
        Assert.Equal(1, equiposDeJ5);
    }

    [Fact]
    public async Task La_familia_de_otro_jugador_no_ve_los_anadidos_ni_los_convocados()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{deA.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([a], e.K1)));

        var deJ3 = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta));
        var deE1 = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(ruta));

        Assert.False(deJ3.TryGetProperty("anadidos", out _));
        Assert.False(deJ3.GetProperty("permisos").GetProperty("dirige").GetBoolean());
        Assert.Equal(1, deJ3.GetProperty("equipos").GetArrayLength());
        Assert.True(deE1.GetProperty("permisos").GetProperty("dirige").GetBoolean());
        Assert.Equal(e.K1.Id, Assert.Single(deE1.GetProperty("anadidos").EnumerateArray()).GetProperty("usuarioRolId").GetGuid());
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.GetAsync($"{ruta}/jugadores"), HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task Quitar_a_un_anadido_no_avisa_ni_deja_respuesta_ni_texto()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{deA.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([a], e.K1)));
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.K1));
        await Actividades.LeerAsync(await e.K1.Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([a])));

        Assert.Equal(HttpStatusCode.NotFound, (await e.K1.Cliente.GetAsync(ruta)).StatusCode);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));
        Assert.Equal(0, await AyudaDeAvisos.SinLeerAsync(e, e.K1));
        var respuestas = await _fabrica.ConContextoAsync(contexto => contexto.RespuestasEntrenamiento.IgnoreQueryFilters()
            .CountAsync(fila => fila.EntrenamientoId == deA.Id && fila.UsuarioRolId == e.K1.Id));
        Assert.Equal(0, respuestas);
    }

    [Fact]
    public async Task Quien_lo_ve_y_no_dirige_recibe_403_y_quien_no_lo_ve_404()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deTodos = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var de2015 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Manana);
        var rutaTodos = e.Ruta($"/entrenamientos/{deTodos.Id}/audiencia");

        // D2 (directivo sin asignar) ve el de toda la categoría y no lo dirige.
        await Actividades.ErrorAsync(
            await e.D2.Cliente.PutAsync(rutaTodos, Audiencia([a])), HttpStatusCode.Forbidden, "no_gestiona_actividad");
        // E1 entrena solo la 2014: no ve el de la 2015.
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{de2015.Id}/audiencia"), Audiencia([])),
            HttpStatusCode.NotFound,
            "no_encontrado");
        // La cuenta de un jugador nunca.
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PutAsync(rutaTodos, Audiencia([])), HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task Un_entrenamiento_cancelado_se_rechaza_y_un_retirado_no_se_suma()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014], Manana, estado: EstadoEntrenamiento.CANCELADO);
        var vigente = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        await _fabrica.Categorias.RetirarAsync(e.K1.Integrante, e.P.Integrante);

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{cancelado.Id}/audiencia"), Audiencia([])),
            HttpStatusCode.Conflict,
            "actividad_cancelada");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{vigente.Id}/audiencia"), Audiencia([], e.K1)),
            HttpStatusCode.Conflict,
            "jugador_fuera_de_la_actividad");
    }

    [Fact]
    public async Task Sumar_a_quien_ya_es_de_la_audiencia_natural_se_acepta_sin_duplicarlo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);

        var cambiado = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{deA.Id}/audiencia"), Audiencia([a], e.Jn(3), e.K1)));

        Assert.Equal(e.K1.Id, Assert.Single(cambiado.GetProperty("anadidos").EnumerateArray()).GetProperty("usuarioRolId").GetGuid());
        var filas = await _fabrica.ConContextoAsync(contexto => contexto.EntrenamientosJugador.IgnoreQueryFilters()
            .CountAsync(fila => fila.EntrenamientoId == deA.Id));
        Assert.Equal(1, filas);
    }

    [Fact]
    public async Task Quien_entrena_solo_una_de_las_categorias_cambia_la_audiencia_pero_no_la_hora()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var conjunto = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/entrenamientos/{conjunto.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([], e.M1)));
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014, e.C2015], lugar: "Otro")),
            HttpStatusCode.Forbidden,
            "no_gestiona_actividad");
    }

    [Fact]
    public async Task Cancelar_y_reactivar_no_cambian_la_audiencia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{deA.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([a], e.K1)));

        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        await Actividades.LeerAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"));

        var detalle = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(ruta));
        Assert.Equal(1, detalle.GetProperty("equipos").GetArrayLength());
        Assert.Equal(1, detalle.GetProperty("anadidos").GetArrayLength());
    }

    [Fact]
    public async Task Cambiar_los_equipos_conserva_el_equipo_desactivado_que_ya_tenia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{deA.Id}");
        await Actividades.LeerAsync(await e.P.Cliente.PostAsync(
            e.Ruta($"/categorias/{e.C2014.Id}/equipos/{a.Id}/desactivacion"), null));

        var cambiado = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/audiencia", Audiencia([b])));

        var nombres = cambiado.GetProperty("equipos").EnumerateArray()
            .ToDictionary(equipo => equipo.GetProperty("nombre").GetString()!, equipo => equipo.GetProperty("activo").GetBoolean());
        Assert.Equal(new Dictionary<string, bool> { ["A"] = false, ["B"] = true }, nombres);
        // Los congelados de A no son "añadidos": siguen viéndolo como audiencia natural.
        Assert.Equal(0, cambiado.GetProperty("anadidos").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task Un_equipo_no_valido_en_la_audiencia_se_rechaza_con_422()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2015 = await _fabrica.Categorias.CrearEquipoAsync(e.C2015, "A");
        var vigente = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{vigente.Id}/audiencia"), Audiencia([de2015])),
            HttpStatusCode.UnprocessableEntity,
            "equipo_no_valido");
    }
}
