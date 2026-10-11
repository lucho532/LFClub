using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de consultar un entrenamiento y los próximos entrenamientos del inicio
/// (spec 009, RF-003, RF-006).
/// Su responsabilidad es responder solo con lo que quien pregunta puede ver, y el mismo 404 para lo
/// que no ve, no existe o es de otro club.
/// No modifica datos ni conoce HTTP.
/// </summary>
public interface IServicioConsultaEntrenamientos
{
    /// <summary>El detalle de un entrenamiento visible.</summary>
    Task<EntrenamientoDto> ObtenerAsync(Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Hasta cinco entrenamientos visibles que no han terminado, cancelados incluidos.</summary>
    Task<IReadOnlyList<EntrenamientoResumenDto>> ProximosAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
