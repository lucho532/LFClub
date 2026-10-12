using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta con la que una familia ve la categoría de su jugador (constitución §7.5
/// y §8; RF-035).
/// Su responsabilidad es devolver, siempre sobre el propio jugador de quien pregunta, su
/// categoría, los equipos en los que está y el nombre de los entrenadores de esa categoría.
/// No recibe ningún identificador, así que no hay forma de pedir otra categoría ni otro jugador, y
/// no devuelve de un entrenador más que su identificador en el club, su nombre, sus apellidos, los
/// equipos que dirige y la versión de su foto (RF-014 de la 008).
/// </summary>
public interface IServicioMiCategoria
{
    /// <summary>La categoría del jugador de quien pregunta, o sin categoría si todavía no tiene.</summary>
    Task<MiCategoriaDto> ObtenerAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
