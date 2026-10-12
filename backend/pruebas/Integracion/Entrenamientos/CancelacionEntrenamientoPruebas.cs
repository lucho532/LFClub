using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Cambiar, cancelar y reactivar un entrenamiento: solo quien lo gestiona, un cancelado sigue a la
/// vista de los mismos, y se reactiva mientras no haya empezado (historia 1, escenarios 1.5, 1.6 y
/// 1.10; RF-002, RF-004, RF-035, RF-038).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CancelacionEntrenamientoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public CancelacionEntrenamientoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Quien_lo_gestiona_cambia_el_lugar_y_las_familias_leen_el_nuevo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var cambio = await e.E2.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014, e.C2015], lugar: "Coliseo"));

        Assert.Equal("Coliseo", (await Actividades.LeerAsync(cambio)).GetProperty("lugar").GetString());
        foreach (var familia in new[] { e.Jn(3), e.K1 })
        {
            Assert.Equal("Coliseo", (await Actividades.LeerAsync(await familia.Cliente.GetAsync(ruta))).GetProperty("lugar").GetString());
        }
    }

    [Fact]
    public async Task Quien_no_entrena_todas_sus_categorias_lo_ve_sin_poder_cambiarlo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var detalle = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(ruta));
        Assert.False(detalle.GetProperty("permisos").GetProperty("gestiona").GetBoolean());

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014, e.C2015])),
            HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"), HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"), HttpStatusCode.Forbidden, "no_gestiona_actividad");
    }

    [Fact]
    public async Task Las_categorias_no_se_cambian()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);

        await Actividades.ErrorAsync(
            await e.E2.Cliente.PutAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}"), Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Conflict, "categorias_no_cambian");
    }

    [Fact]
    public async Task Cancelado_lo_siguen_viendo_los_mismos_y_no_admite_cambios_ni_otra_cancelacion()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var cancelado = await Actividades.LeerAsync(await e.E2.Cliente.PostAsync($"{ruta}/cancelacion"));

        Assert.Equal("CANCELADO", cancelado.GetProperty("estado").GetString());
        foreach (var persona in new[] { e.Jn(3), e.K1, e.P, e.D2 })
        {
            Assert.Equal("CANCELADO", (await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta))).GetProperty("estado").GetString());
        }

        await Actividades.ErrorAsync(await e.E2.Cliente.PostAsync($"{ruta}/cancelacion"), HttpStatusCode.Conflict, "actividad_cancelada");
        await Actividades.ErrorAsync(
            await e.E2.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014, e.C2015])),
            HttpStatusCode.Conflict, "actividad_cancelada");
        Assert.True(await _fabrica.ConContextoAsync(contexto =>
            contexto.Entrenamientos.IgnoreQueryFilters().AnyAsync(fila => fila.Id == entrenamiento.Id)));
    }

    [Fact]
    public async Task Reactivado_antes_de_empezar_vuelve_a_programado_para_todos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014, e.C2015], Manana, estado: EstadoEntrenamiento.CANCELADO);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var reactivado = await Actividades.LeerAsync(await e.E2.Cliente.DeleteAsync($"{ruta}/cancelacion"));

        Assert.Equal("PROGRAMADO", reactivado.GetProperty("estado").GetString());
        foreach (var persona in new[] { e.Jn(3), e.K1, e.P, e.D2 })
        {
            Assert.Equal("PROGRAMADO", (await Actividades.LeerAsync(await persona.Cliente.GetAsync(ruta))).GetProperty("estado").GetString());
        }

        await Actividades.ErrorAsync(await e.E2.Cliente.DeleteAsync($"{ruta}/cancelacion"), HttpStatusCode.Conflict, "actividad_no_cancelada");
    }

    [Fact]
    public async Task Ya_empezado_no_se_reactiva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014], Manana, new TimeOnly(16, 0), new TimeOnly(17, 30), EstadoEntrenamiento.CANCELADO);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}/cancelacion");

        _fabrica.Reloj.FijarEnColombia(Manana, new TimeOnly(16, 0));

        await Actividades.ErrorAsync(await e.E1.Cliente.DeleteAsync(ruta), HttpStatusCode.Conflict, "actividad_empezada");
    }

    [Fact]
    public async Task Con_una_categoria_desactivada_no_se_reactiva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var c2012 = await _fabrica.Categorias.CrearCategoriaAsync(e.Club, 2012);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014, c2012], Manana, estado: EstadoEntrenamiento.CANCELADO);
        await _fabrica.ConContextoAsync(contexto => contexto.Categorias.IgnoreQueryFilters()
            .Where(categoria => categoria.Id == c2012.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(categoria => categoria.Activa, false)));

        await Actividades.ErrorAsync(
            await e.P.Cliente.DeleteAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}/cancelacion")),
            HttpStatusCode.Conflict, "categoria_inactiva");
    }
}
