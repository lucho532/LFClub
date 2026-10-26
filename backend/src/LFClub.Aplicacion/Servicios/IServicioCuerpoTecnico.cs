using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta del cuerpo técnico del club por su PRESIDENTE (spec 018): los PRESIDENTE,
/// DIRECTIVO y ENTRENADOR del club con sus categorías y, abriendo a uno, todos sus datos y los próximos
/// entrenamientos de sus categorías.
/// Su responsabilidad es leer y convertir en DTO; responde como si no existiera cuando la persona es de otro
/// club, es JUGADOR, no está aprobada o no existe.
/// No decide quién puede consultarlo (lo hace la autorización) y no modifica datos.
/// </summary>
public interface IServicioCuerpoTecnico
{
    /// <summary>El cuerpo técnico del club, por rol (presidentes, directivos, entrenadores) y nombre.</summary>
    Task<IReadOnlyList<MiembroCuerpoTecnicoDto>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Todos los datos de una persona del cuerpo técnico; 404 <c>no_encontrado</c> si no existe aquí.</summary>
    Task<MiembroCuerpoTecnicoDetalleDto> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);
}
