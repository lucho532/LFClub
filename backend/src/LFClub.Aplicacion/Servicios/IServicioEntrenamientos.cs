using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de programar, cambiar, cancelar y reactivar entrenamientos (spec 009,
/// historia 1).
/// Su responsabilidad es que solo los gestione el PRESIDENTE o quien entrena todas sus categorías,
/// con categorías activas del club, y que cada cambio de estado respete el momento del
/// entrenamiento. Todas responden con el detalle tal como lo ve quien lo pidió.
/// No conoce HTTP y nunca borra un entrenamiento (RF-035).
/// </summary>
public interface IServicioEntrenamientos
{
    /// <summary>Programa un entrenamiento para una o varias categorías.</summary>
    Task<EntrenamientoDto> ProgramarAsync(
        GuardarEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Cambia fecha, horas, lugar y nota de un entrenamiento programado; las categorías no cambian.</summary>
    Task<EntrenamientoDto> CambiarAsync(
        Guid entrenamientoId, GuardarEntrenamientoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Cancela un entrenamiento programado.</summary>
    Task<EntrenamientoDto> CancelarAsync(Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Reactiva un entrenamiento cancelado que todavía no empieza y cuyas categorías siguen activas.</summary>
    Task<EntrenamientoDto> ReactivarAsync(Guid entrenamientoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
