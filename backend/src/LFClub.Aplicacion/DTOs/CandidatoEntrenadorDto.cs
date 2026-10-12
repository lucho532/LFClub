using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un integrante que el PRESIDENTE puede asignar como entrenador de una categoría.
/// Su responsabilidad es identificarlo y decir su rol en el club.
/// No lleva datos de contacto.
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante en este club.</param>
/// <param name="Nombres">Nombres del integrante.</param>
/// <param name="Apellidos">Apellidos del integrante.</param>
/// <param name="Rol">Su rol en el club: ENTRENADOR, DIRECTIVO o PRESIDENTE.</param>
public record CandidatoEntrenadorDto(Guid UsuarioRolId, string Nombres, string Apellidos, Rol Rol);
