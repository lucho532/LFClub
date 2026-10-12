using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso del calendario: los entrenamientos y los partidos que quien pregunta
/// puede ver en un rango de días (spec 009, RF-003, RF-010).
/// Su responsabilidad es validar el rango y no devolver nada que quien pregunta no pueda ver.
/// No modifica datos ni conoce HTTP.
/// </summary>
public interface IServicioCalendario
{
    /// <summary>El calendario de ese rango, en fechas <c>AAAA-MM-DD</c>.</summary>
    Task<CalendarioDto> ObtenerAsync(
        string? desde, string? hasta, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
