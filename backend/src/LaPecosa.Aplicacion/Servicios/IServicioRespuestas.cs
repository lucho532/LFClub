using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de la familia que dice si su jugador asistirá (spec 009, historia 5).
/// Su responsabilidad es guardar la respuesta del jugador de la petición a un entrenamiento de su
/// categoría o a un partido al que está convocado, mientras siga en pie y no haya empezado.
/// No conoce HTTP ni registra la asistencia real (RF-025).
/// </summary>
public interface IServicioRespuestas
{
    /// <summary>Responde a un entrenamiento y devuelve su detalle con la respuesta.</summary>
    Task<EntrenamientoDto> ResponderEntrenamientoAsync(
        Guid entrenamientoId, ResponderDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Responde a un partido y devuelve su detalle con la respuesta.</summary>
    Task<PartidoDto> ResponderPartidoAsync(
        Guid partidoId, ResponderDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
