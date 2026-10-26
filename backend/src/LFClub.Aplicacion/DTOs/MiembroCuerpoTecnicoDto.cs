using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una persona del cuerpo técnico en la lista que ve el PRESIDENTE (spec 018).
/// Su responsabilidad es llevar su nombre, su rol en el club, la versión de su foto y las categorías activas
/// que entrena, con los equipos activos que dirige.
/// No lleva documento ni datos de contacto: esos solo salen en el detalle.
/// </summary>
/// <param name="UsuarioRolId">Integrante de este club.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Rol">PRESIDENTE, DIRECTIVO o ENTRENADOR; nunca JUGADOR.</param>
/// <param name="VersionFoto">Versión de su foto de perfil; 0 si no tiene y se muestran sus iniciales.</param>
/// <param name="Categorias">Categorías activas que entrena en este club; vacía si no entrena.</param>
public record MiembroCuerpoTecnicoDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    Rol Rol,
    int VersionFoto,
    IReadOnlyList<CategoriaDeFichaBasicaDto> Categorias);
