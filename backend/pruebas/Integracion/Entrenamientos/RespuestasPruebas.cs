using System.Net;
using System.Text.Json;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Las respuestas de las familias: cada cuenta responde por su jugador elegido hasta la hora de
/// inicio, quien entrena ve conteos y nombres, ninguna familia ve las de otras y la asistencia real
/// es un dato aparte (historia 5, escenarios 5.1 a 5.5; RF-023 a RF-025).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RespuestasPruebas : IDisposable
{
    private static readonly object Asistira = new { respuesta = "ASISTIRA" };
    private static readonly object NoAsistira = new { respuesta = "NO_ASISTIRA" };

    private readonly FabricaApi _fabrica;

    public RespuestasPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    private static JsonElement Fila(JsonElement planilla, string lista, Persona jugador) =>
        planilla.GetProperty(lista).EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == jugador.Id);

    [Fact]
    public async Task Quien_entrena_ve_los_conteos_y_ninguna_familia_ve_la_respuesta_de_otra()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var deJ3 = await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));
        Assert.Equal("ASISTIRA", deJ3.GetProperty("miRespuesta").GetString());
        await Actividades.LeerAsync(await e.Jn(4).Cliente.PutAsync($"{ruta}/mi-respuesta", NoAsistira));

        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        Assert.Equal(1, planilla.GetProperty("asistiran").GetInt32());
        Assert.Equal(1, planilla.GetProperty("noAsistiran").GetInt32());
        Assert.Equal(planilla.GetProperty("jugadores").GetArrayLength() - 2, planilla.GetProperty("sinResponder").GetInt32());
        Assert.Equal("NO_ASISTIRA", Fila(planilla, "jugadores", e.Jn(4)).GetProperty("respuesta").GetString());

        // J3 cambia de opinión; la familia de J4 no ve la de J3.
        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync($"{ruta}/mi-respuesta", NoAsistira));
        var texto = await (await e.Jn(4).Cliente.GetAsync(ruta)).Content.ReadAsStringAsync();
        Assert.DoesNotContain(e.Jn(3).Integrante.Apellidos, texto);
        Assert.DoesNotContain("noAsistiran", texto);
        Assert.Equal(2, (await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"))).GetProperty("noAsistiran").GetInt32());
    }

    [Fact]
    public async Task Solo_responde_la_cuenta_de_un_jugador_de_la_categoria_antes_de_empezar()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana, new TimeOnly(16, 0));
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014], Manana, estado: LaPecosa.Dominio.Enumeraciones.EstadoEntrenamiento.CANCELADO);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}/mi-respuesta");

        await Actividades.ErrorAsync(await e.M1.Cliente.PutAsync(ruta, Asistira), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(await e.E1.Cliente.PutAsync(ruta, Asistira), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PutAsync(e.Ruta($"/entrenamientos/{cancelado.Id}/mi-respuesta"), Asistira),
            HttpStatusCode.Conflict, "actividad_cancelada");

        _fabrica.Reloj.FijarEnColombia(entrenamiento.Fecha, new TimeOnly(16, 0));
        await Actividades.ErrorAsync(await e.Jn(3).Cliente.PutAsync(ruta, Asistira), HttpStatusCode.Conflict, "actividad_empezada");
    }

    [Fact]
    public async Task Los_hermanos_responden_cada_uno_por_lo_suyo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var de2015 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Manana);

        await Actividades.LeerAsync(await e.Jn(1).Cliente.PutAsync(e.Ruta($"/entrenamientos/{de2014.Id}/mi-respuesta"), Asistira));
        await Actividades.LeerAsync(await e.Jn(2).Cliente.PutAsync(e.Ruta($"/entrenamientos/{de2015.Id}/mi-respuesta"), NoAsistira));
        await Actividades.ErrorAsync(
            await e.Jn(2).Cliente.PutAsync(e.Ruta($"/entrenamientos/{de2014.Id}/mi-respuesta"), Asistira),
            HttpStatusCode.NotFound, "no_encontrado");

        var deE2 = await Actividades.LeerAsync(await e.E2.Cliente.GetAsync(e.Ruta($"/entrenamientos/{de2015.Id}/jugadores")));
        Assert.Equal("NO_ASISTIRA", Fila(deE2, "jugadores", e.Jn(2)).GetProperty("respuesta").GetString());
    }

    [Fact]
    public async Task La_asistencia_real_no_borra_la_respuesta_y_reactivar_la_conserva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hoy = _fabrica.Reloj.Hoy;
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], hoy, new TimeOnly(16, 0), new TimeOnly(17, 30));
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");
        _fabrica.Reloj.FijarEnColombia(hoy, new TimeOnly(8, 0));

        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        await Actividades.LeerAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"));
        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/asistencia", new { jugadores = new[] { new { usuarioRolId = e.Jn(3).Id, presente = false } } }));

        var j3 = Fila(planilla, "jugadores", e.Jn(3));
        Assert.Equal("ASISTIRA", j3.GetProperty("respuesta").GetString());
        Assert.False(j3.GetProperty("presente").GetBoolean());
        var detalle = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta));
        Assert.Equal("ASISTIRA", detalle.GetProperty("miRespuesta").GetString());
        Assert.False(detalle.GetProperty("miAsistencia").GetBoolean());
    }

    [Fact]
    public async Task En_un_partido_responde_el_convocado_y_salir_de_la_convocatoria_borra_su_respuesta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana, convocados: [e.Jn(3).Integrante]);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        var respondido = await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));
        Assert.Equal("ASISTIRA", respondido.GetProperty("miRespuesta").GetString());
        await Actividades.ErrorAsync(await e.Jn(4).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira), HttpStatusCode.NotFound, "no_encontrado");
        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        Assert.Equal("ASISTIRA", Fila(planilla, "convocados", e.Jn(3)).GetProperty("respuesta").GetString());

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", new { usuarioRolIds = Array.Empty<Guid>() }));
        planilla = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync($"{ruta}/convocatoria", new { usuarioRolIds = new[] { e.Jn(3).Id } }));

        Assert.Equal(JsonValueKind.Null, Fila(planilla, "convocados", e.Jn(3)).GetProperty("respuesta").ValueKind);
        Assert.Equal(1, planilla.GetProperty("sinResponder").GetInt32());
    }
}
