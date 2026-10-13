using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la validación y el armado de la audiencia de un entrenamiento (spec 013, RF-001,
/// RF-013, RF-026; research §10).
/// Su responsabilidad es comprobar que cada equipo enviado existe en el club, está activo y es de
/// una de las categorías activas de la actividad (si no, <c>equipo_no_valido</c>); conservar los
/// equipos desactivados que el entrenamiento ya tenía; comprobar que cada añadido es un jugador
/// activo, aprobado, no retirado y con categoría del club (si no, <c>jugador_fuera_de_la_actividad</c>);
/// y quitar de los añadidos a quien ya es de la audiencia natural, para no contarlo dos veces.
/// También comprueba que un cambio de datos no intente cambiar la audiencia (research §5).
/// No guarda nada, no consulta Entity Framework (usa <see cref="IRepositorioAudiencia"/>) y no
/// decide quién puede cambiar la audiencia.
/// </summary>
public class ArmadoDeAudiencia
{
    private readonly IRepositorioAudiencia _audiencia;

    /// <summary>Crea el colaborador con su repositorio.</summary>
    public ArmadoDeAudiencia(IRepositorioAudiencia audiencia)
    {
        _audiencia = audiencia;
    }

    /// <summary>
    /// Exige que los equipos y los añadidos enviados al cambiar los datos de un entrenamiento sean
    /// los que ya tiene (los de la audiencia se cambian con su propia operación). Si no se envían,
    /// no hay nada que comprobar. Si difieren, 422 de validación apuntando a la audiencia.
    /// </summary>
    /// <param name="actual">El entrenamiento, con sus equipos.</param>
    /// <param name="equipoIds">Equipos enviados; los desactivados que ya tenía pueden o no venir.</param>
    /// <param name="jugadorIds">Añadidos enviados.</param>
    /// <param name="cancelacion">Cancelación.</param>
    public async Task ExigirSinCambiosAsync(
        Entrenamiento actual,
        IReadOnlyCollection<Guid>? equipoIds,
        IReadOnlyCollection<Guid>? jugadorIds,
        CancellationToken cancelacion = default)
    {
        var errores = new ErroresDeValidacion();
        if (equipoIds is not null)
        {
            var enviados = equipoIds.ToHashSet();
            var todos = actual.Equipos.Select(fila => fila.EquipoId).ToHashSet();
            var activos = actual.Equipos.Where(fila => fila.Equipo?.Activo != false).Select(fila => fila.EquipoId).ToHashSet();
            if (!enviados.SetEquals(todos) && !enviados.SetEquals(activos))
            {
                errores.Agregar("equipoIds", "Los equipos no se cambian al editar. Usa «Cambiar audiencia».");
            }
        }

        if (jugadorIds is not null)
        {
            var enviados = jugadorIds.ToHashSet();
            var actuales = (await _audiencia.AnadidosAsync(actual.Id, cancelacion)).Select(jugador => jugador.UsuarioRolId);
            if (!enviados.SetEquals(actuales))
            {
                errores.Agregar("jugadorAnadidoIds", "Los jugadores añadidos no se cambian al editar. Usa «Cambiar audiencia».");
            }
        }

        errores.LanzarSiHayErrores();
    }

    /// <summary>
    /// Valida y arma la audiencia.
    /// </summary>
    /// <param name="categoriaIds">Categorías de la actividad.</param>
    /// <param name="equipoIds">Equipos elegidos; ausente o vacío, toda la categoría.</param>
    /// <param name="jugadorIds">Jugadores que se suman; ausente, ninguno.</param>
    /// <param name="equiposConservados">Equipos desactivados que el entrenamiento ya tenía y se conservan.</param>
    /// <param name="cancelacion">Cancelación.</param>
    public async Task<AudienciaArmada> ArmarAsync(
        IReadOnlyCollection<Guid> categoriaIds,
        IReadOnlyCollection<Guid>? equipoIds,
        IReadOnlyCollection<Guid>? jugadorIds,
        IReadOnlyCollection<Guid> equiposConservados,
        CancellationToken cancelacion = default)
    {
        var elegidos = (equipoIds ?? []).Distinct().ToList();
        if (elegidos.Count > 0)
        {
            var existentes = (await _audiencia.EquiposAsync(elegidos, cancelacion)).ToDictionary(equipo => equipo.EquipoId);
            var categorias = categoriaIds.ToHashSet();
            var valido = elegidos.All(id => existentes.TryGetValue(id, out var equipo)
                && equipo.Activo
                && equipo.CategoriaActiva
                && categorias.Contains(equipo.CategoriaId));
            if (!valido)
            {
                throw ErroresDeActividades.EquipoNoValido();
            }
        }

        var equipos = elegidos.Union(equiposConservados).ToList();

        var candidatos = (jugadorIds ?? []).Distinct().ToList();
        if (candidatos.Count == 0)
        {
            return new AudienciaArmada(equipos, []);
        }

        var sumables = await _audiencia.JugadoresSumablesAsync(candidatos, cancelacion);
        if (candidatos.Any(id => !sumables.Contains(id)))
        {
            throw ErroresDeActividades.JugadorFueraDeLaActividad();
        }

        var naturales = await _audiencia.AudienciaNaturalAsync(categoriaIds, equipos, cancelacion);
        return new AudienciaArmada(equipos, candidatos.Where(id => !naturales.Contains(id)).ToList());
    }
}

/// <summary>
/// Representa la audiencia ya validada de un entrenamiento.
/// Su responsabilidad es llevar los equipos que se guardan (los elegidos y los desactivados que se
/// conservan) y los jugadores que se guardan como añadidos.
/// No lleva a la audiencia natural.
/// </summary>
/// <param name="EquipoIds">Equipos que se guardan.</param>
/// <param name="JugadorAnadidoIds">Jugadores que se guardan como añadidos.</param>
public record AudienciaArmada(IReadOnlyList<Guid> EquipoIds, IReadOnlyList<Guid> JugadorAnadidoIds);
