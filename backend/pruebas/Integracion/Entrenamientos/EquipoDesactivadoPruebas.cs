using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Desactivar un equipo congela su audiencia en los entrenamientos próximos: quienes lo veían lo
/// siguen viendo, el entrenamiento no pasa a toda la categoría ni se cancela, los de fecha pasada no
/// se copian y un equipo dirigido por un entrenamiento no se borra (spec 013; RF-026, CE-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EquipoDesactivadoPruebas
{
    private readonly FabricaApi _fabrica;

    public EquipoDesactivadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private static string RutaEquipo(EscenarioActividades e, Guid categoriaId, Guid equipoId) =>
        e.Ruta($"/categorias/{categoriaId}/equipos/{equipoId}");

    [Fact]
    public async Task Al_desactivar_el_equipo_sus_jugadores_siguen_viendo_el_proximo_y_los_de_otros_equipos_no()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var proximo = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(2), [a]);
        var pasado = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(-2), [a]);
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync(
            [e.C2014], Hoy.AddDays(3), [a], estado: EstadoEntrenamiento.CANCELADO);

        await Actividades.LeerAsync(
            await e.P.Cliente.PostAsync($"{RutaEquipo(e, e.C2014.Id, a.Id)}/desactivacion", null));

        // J3, J4 y J6 eran del equipo A y lo siguen viendo; J5 (solo B) y J7 (sin equipo) no.
        foreach (var numero in new[] { 3, 4, 6 })
        {
            Assert.Equal(HttpStatusCode.OK, (await e.Jn(numero).Cliente.GetAsync(e.Ruta($"/entrenamientos/{proximo.Id}"))).StatusCode);
        }

        foreach (var numero in new[] { 5, 7 })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await e.Jn(numero).Cliente.GetAsync(e.Ruta($"/entrenamientos/{proximo.Id}"))).StatusCode);
        }

        // No pasa a toda la categoría, no se cancela, y el detalle conserva el equipo marcado como no activo.
        var detalle = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{proximo.Id}")));
        Assert.Equal("PROGRAMADO", detalle.GetProperty("estado").GetString());
        var equipo = Assert.Single(detalle.GetProperty("equipos").EnumerateArray());
        Assert.False(equipo.GetProperty("activo").GetBoolean());

        // Solo se copia a los próximos; el cancelado también (se puede reactivar); el pasado, no.
        var copias = await _fabrica.ConContextoAsync(contexto => contexto.EntrenamientosJugador.IgnoreQueryFilters()
            .Where(fila => fila.Origen == OrigenDeJugadorEnEntrenamiento.EQUIPO_DESACTIVADO && fila.EquipoDeOrigenId == a.Id)
            .Select(fila => fila.EntrenamientoId).Distinct().ToListAsync());
        Assert.Contains(proximo.Id, copias);
        Assert.Contains(cancelado.Id, copias);
        Assert.DoesNotContain(pasado.Id, copias);
    }

    [Fact]
    public async Task Un_equipo_dirigido_por_un_entrenamiento_se_desactiva_pero_no_se_borra()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(e.C2014, "C");
        var creado = await Actividades.LeerAsync(
            await e.P.Cliente.PostAsync(
                e.Ruta("/entrenamientos"),
                Actividades.Entrenamiento(Hoy.AddDays(1), [e.C2014], equipos: [equipo])),
            HttpStatusCode.Created);

        var intento = await e.P.Cliente.DeleteAsync(RutaEquipo(e, e.C2014.Id, equipo.Id));

        Assert.Equal(HttpStatusCode.Conflict, intento.StatusCode);
        Assert.NotEqual(Guid.Empty, creado.GetProperty("entrenamientoId").GetGuid());
    }
}
