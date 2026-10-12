using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa las consultas del apartado "Categorías" (constitución §7.5 y §12.3; RF-031 a RF-034
/// y RF-044).
/// Su responsabilidad es devolver a cada quien las categorías que le corresponden según su rol,
/// con sus equipos, sus entrenadores y sus jugadores, y las listas "Sin categoría" y "Retirados".
/// No modifica nada y no da a un PRESIDENTE o a un DIRECTIVO un alcance distinto por estar
/// asignado como entrenador (RF-017a).
/// </summary>
public interface IServicioConsultaCategorias
{
    /// <summary>Las categorías que ve ese integrante, por año.</summary>
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Una categoría con sus jugadores. Responde 404 si no existe en el club o si ese integrante
    /// no puede verla, sin distinguir un caso del otro.
    /// </summary>
    Task<CategoriaDetalleDto> ObtenerAsync(
        Guid categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Jugadores aprobados y no retirados del club que todavía no tienen categoría.</summary>
    Task<IReadOnlyList<JugadorDeCategoriaDto>> ListarSinCategoriaAsync(CancellationToken cancelacion = default);

    /// <summary>Jugadores retirados del club, del retiro más reciente al más antiguo.</summary>
    Task<IReadOnlyList<JugadorRetiradoDto>> ListarRetiradosAsync(CancellationToken cancelacion = default);
}
