using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de cambiar a quién va dirigido un entrenamiento: sus equipos y los
/// jugadores añadidos (spec 013, historias 3 y 4; RF-016, RF-020, RF-022).
/// Su responsabilidad es reemplazar los equipos activos y los añadidos conservando los equipos
/// desactivados, avisar a quienes pasan a verlo y no dejar ningún rastro en quienes dejan de verlo.
/// No cambia los datos del entrenamiento ni lo cancela.
/// </summary>
public interface IServicioAudienciaDeEntrenamiento
{
    /// <summary>
    /// Cambia la audiencia de un entrenamiento programado y devuelve su detalle. Lanza
    /// <c>404 no_encontrado</c> si quien pregunta no lo ve, <c>403 no_gestiona_actividad</c> si lo ve
    /// pero no lo dirige y <c>409 actividad_cancelada</c> si está cancelado.
    /// </summary>
    Task<EntrenamientoDto> CambiarAsync(
        Guid entrenamientoId, AudienciaDeEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
