using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// La parte del sembrador de actividades que escribe la audiencia de un entrenamiento (spec 013):
/// los equipos a los que va dirigido y los jugadores añadidos o congelados, directamente en la
/// base de datos, para las pruebas que solo la leen.
/// </summary>
public partial class SembradorActividades
{
    /// <summary>
    /// Los equipos A y B de la 2014 del escenario: A con J3, J4 y J6; B con J5 y J6; J7 sin equipo.
    /// </summary>
    public async Task<(Equipo A, Equipo B)> CrearEquiposAsync(EscenarioActividades e)
    {
        var categorias = _fabrica.Categorias;
        var a = await categorias.CrearEquipoAsync(e.C2014, "A");
        var b = await categorias.CrearEquipoAsync(e.C2014, "B");
        foreach (var numero in new[] { 3, 4, 6 })
        {
            await categorias.PonerEnEquipoAsync(e.Jn(numero).Integrante, a);
        }

        foreach (var numero in new[] { 5, 6 })
        {
            await categorias.PonerEnEquipoAsync(e.Jn(numero).Integrante, b);
        }

        return (a, b);
    }

    /// <summary>Dirige el entrenamiento a esos equipos y los marca como usados.</summary>
    public Task DirigirAEquiposAsync(Entrenamiento entrenamiento, params Equipo[] equipos) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            foreach (var equipo in equipos)
            {
                contexto.EntrenamientosEquipo.Add(new EntrenamientoEquipo
                {
                    EntrenamientoId = entrenamiento.Id,
                    EquipoId = equipo.Id,
                    ClubId = entrenamiento.ClubId,
                });
            }

            var ids = equipos.Select(equipo => equipo.Id).ToList();
            await contexto.Equipos.IgnoreQueryFilters()
                .Where(equipo => ids.Contains(equipo.Id))
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Usado, true));
            return await contexto.SaveChangesAsync();
        });

    /// <summary>Suma al jugador a la lista guardada del entrenamiento.</summary>
    public Task AnadirJugadorAsync(
        Entrenamiento entrenamiento,
        UsuarioRol jugador,
        OrigenDeJugadorEnEntrenamiento origen = OrigenDeJugadorEnEntrenamiento.ANADIDO,
        Equipo? equipoDeOrigen = null) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.EntrenamientosJugador.Add(new EntrenamientoJugador
            {
                EntrenamientoId = entrenamiento.Id,
                UsuarioRolId = jugador.Id,
                ClubId = entrenamiento.ClubId,
                Origen = origen,
                EquipoDeOrigenId = equipoDeOrigen?.Id,
                AnadidoEn = DateTime.UtcNow,
            });
            return await contexto.SaveChangesAsync();
        });

    /// <summary>Entrenamiento escrito en la base, dirigido a esos equipos de sus categorías.</summary>
    public async Task<Entrenamiento> CrearEntrenamientoDeEquiposAsync(
        IEnumerable<Categoria> categorias,
        DateOnly fecha,
        IEnumerable<Equipo> equipos,
        TimeOnly? horaInicio = null,
        EstadoEntrenamiento estado = EstadoEntrenamiento.PROGRAMADO)
    {
        var entrenamiento = await CrearEntrenamientoAsync(
            categorias, fecha, horaInicio, horaInicio?.AddHours(1), estado);
        await DirigirAEquiposAsync(entrenamiento, equipos.ToArray());
        return entrenamiento;
    }
}
