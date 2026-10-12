using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de la planilla y la asistencia de un entrenamiento (spec 009,
/// historia 4).
/// Su responsabilidad es mostrar la planilla solo al PRESIDENTE y a quien entrena alguna de sus
/// categorías, con los jugadores de las categorías que puede marcar, y registrar la asistencia desde
/// el día del entrenamiento.
/// No conoce HTTP y nunca entrega la planilla a la cuenta de un jugador.
/// </summary>
public interface IServicioAsistencia
{
    /// <summary>La planilla del entrenamiento.</summary>
    Task<PlanillaEntrenamientoDto> PlanillaAsync(Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Registra o corrige la asistencia de los jugadores enviados y responde con la planilla.</summary>
    Task<PlanillaEntrenamientoDto> RegistrarAsync(
        Guid entrenamientoId, RegistrarAsistenciaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
