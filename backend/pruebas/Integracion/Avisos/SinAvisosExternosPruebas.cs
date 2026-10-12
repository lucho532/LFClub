using LFClub.Aplicacion.Implementaciones;
using LFClub.Aplicacion.Interfaces;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.Extensions.DependencyInjection;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Ningún aviso de actividad sale de la aplicación: ni correo ni ningún otro envío (spec 012,
/// RF-027 y CE-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SinAvisosExternosPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public SinAvisosExternosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose()
    {
        _fabrica.Correo.FallarEnvios = false;
        _fabrica.Reloj.UsarElReal();
    }

    [Fact]
    public async Task Programar_cambiar_cancelar_reactivar_asistencia_convocar_y_resultado_no_envian_ningun_correo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hoy = _fabrica.Reloj.Hoy;
        var antes = _fabrica.Correo.Enviados.Count;

        // Entrenamiento: programar, cambiar, cancelar, reactivar y registrar asistencia.
        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(hoy, [e.C2014])),
            System.Net.HttpStatusCode.Created);
        var ruta = e.Ruta($"/entrenamientos/{creado.GetProperty("entrenamientoId").GetGuid()}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Entrenamiento(hoy, [e.C2014], lugar: "Coliseo")));
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        await Actividades.LeerAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"));
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/asistencia", new { jugadores = new[] { new { usuarioRolId = e.Jn(3).Id, presente = true } } }));

        // Partido: convocar, cambiar y resultado del equipo.
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], hoy.AddDays(1));
        var rutaPartido = e.Ruta($"/partidos/{partido.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{rutaPartido}/convocatoria", new { usuarioRolIds = new[] { e.Jn(3).Id } }));
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            rutaPartido, Actividades.Partido(hoy.AddDays(1), [e.C2014], hora: "11:00")));

        Assert.Equal(antes, _fabrica.Correo.Enviados.Count);
        Assert.NotEmpty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }

    [Fact]
    public async Task Los_avisos_funcionan_aunque_el_servicio_de_correo_falle()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        _fabrica.Correo.FallarEnvios = true;

        await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(
                e.Ruta("/entrenamientos"), Actividades.Entrenamiento(_fabrica.Reloj.Hoy.AddDays(1), [e.C2014])),
            System.Net.HttpStatusCode.Created);

        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }

    [Fact]
    public void El_servicio_de_avisos_no_depende_del_servicio_de_correo()
    {
        var constructores = typeof(ServicioAvisosDeActividad).GetConstructors();

        var parametros = constructores.SelectMany(constructor => constructor.GetParameters()).Select(p => p.ParameterType);
        Assert.DoesNotContain(typeof(IServicioCorreo), parametros);
        Assert.NotNull(_fabrica.Services.GetRequiredService<IServicioCorreo>());
    }
}
