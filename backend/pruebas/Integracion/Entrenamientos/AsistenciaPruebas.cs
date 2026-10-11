using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// La asistencia: quien entrena marca solo a los de sus categorías, el PRESIDENTE a todos, desde el
/// día del entrenamiento; la cuenta del jugador ve solo la suya, y la categoría copiada no cambia
/// (historia 4, escenarios 4.1 a 4.5; RF-022, RF-024; CE-002, CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AsistenciaPruebas
{
    private readonly FabricaApi _fabrica;

    public AsistenciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private static object Marcar(params (Persona Jugador, bool Presente)[] filas) =>
        new { jugadores = filas.Select(fila => new { usuarioRolId = fila.Jugador.Id, presente = fila.Presente }) };

    private static JsonElement Fila(JsonElement planilla, Persona jugador) =>
        planilla.GetProperty("jugadores").EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == jugador.Id);

    [Fact]
    public async Task Quien_entrena_una_sola_categoria_ve_y_marca_solo_la_suya()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var deE1 = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        var ids = Actividades.Ids(deE1.GetProperty("jugadores"), "usuarioRolId");
        Assert.Equal(9, ids.Count);
        Assert.DoesNotContain(e.K1.Id, ids);
        Assert.DoesNotContain(e.Jn(2).Id, ids);
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.K1, true))), HttpStatusCode.Conflict, "jugador_fuera_de_la_actividad");

        var marcada = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), true), (e.Jn(4), false))));
        Assert.True(Fila(marcada, e.Jn(3)).GetProperty("presente").GetBoolean());
        Assert.Equal(JsonValueKind.Null, Fila(marcada, e.Jn(5)).GetProperty("presente").ValueKind);

        // E2 entrena las dos: ve a todos y corrige a uno de la 2014.
        var deE2 = await Actividades.LeerAsync(await e.E2.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), false), (e.K1, true))));
        Assert.Equal(11, deE2.GetProperty("jugadores").GetArrayLength());
        Assert.False(Fila(deE2, e.Jn(3)).GetProperty("presente").GetBoolean());

        var deP = await Actividades.LeerAsync(await e.P.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(2), true))));
        Assert.Equal(11, deP.GetProperty("jugadores").GetArrayLength());
    }

    [Fact]
    public async Task Ni_el_directivo_sin_asignar_ni_la_cuenta_de_un_jugador_reciben_la_planilla()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.ErrorAsync(await e.D2.Cliente.GetAsync($"{ruta}/jugadores"), HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(
            await e.D2.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), true))), HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(await e.Jn(3).Cliente.GetAsync($"{ruta}/jugadores"), HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task Un_entrenamiento_de_manana_o_cancelado_no_admite_asistencia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var manana = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(1));
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy, estado: EstadoEntrenamiento.CANCELADO);

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{manana.Id}/asistencia"), Marcar((e.Jn(3), true))),
            HttpStatusCode.Conflict, "entrenamiento_futuro");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(e.Ruta($"/entrenamientos/{cancelado.Id}/asistencia"), Marcar((e.Jn(3), true))),
            HttpStatusCode.Conflict, "actividad_cancelada");
        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{manana.Id}/jugadores")));
        Assert.False(Fila(planilla, e.Jn(3)).GetProperty("marcable").GetBoolean());
    }

    [Fact]
    public async Task El_jugador_ve_solo_su_propia_asistencia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), true), (e.Jn(4), false))));

        var texto = await (await e.Jn(3).Cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        var deJ3 = JsonDocument.Parse(texto).RootElement;

        Assert.True(deJ3.GetProperty("miAsistencia").GetBoolean());
        Assert.False((await Actividades.LeerAsync(await e.Jn(4).Cliente.GetAsync(ruta))).GetProperty("miAsistencia").GetBoolean());
        Assert.DoesNotContain(e.Jn(4).Integrante.Apellidos, texto);
        Assert.DoesNotContain("jugadores", texto);
    }

    [Fact]
    public async Task La_categoria_copiada_no_cambia_y_el_jugador_sigue_viendo_el_entrenamiento()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), true))));

        await _fabrica.Actividades.CambiarCategoriaAsync(e.Jn(3).Integrante, e.C2013);
        var corregida = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/asistencia", Marcar((e.Jn(3), false))));

        var j3 = Fila(corregida, e.Jn(3));
        Assert.Equal(2014, j3.GetProperty("categoria").GetProperty("anio").GetInt32());
        Assert.False(j3.GetProperty("presente").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);
    }
}
