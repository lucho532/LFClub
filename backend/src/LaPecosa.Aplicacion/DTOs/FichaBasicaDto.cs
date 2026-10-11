using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa la ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR de un club (spec 008,
/// RF-001 y RF-002; constitución §12.6).
/// Su responsabilidad es llevar lo que cualquier integrante del club puede ver de esa persona: su
/// nombre, su rol en este club, la versión de su foto de perfil y las categorías activas que entrena
/// aquí, con los equipos activos que dirige.
/// Nunca lleva documento, fecha de nacimiento, correo, celular ni responsable, ni nada de otro club
/// (RF-005). Se arma al consultar: no es una tabla.
/// </summary>
/// <param name="UsuarioRolId">Integrante de este club.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Rol">PRESIDENTE, DIRECTIVO o ENTRENADOR; nunca JUGADOR.</param>
/// <param name="VersionFoto">Versión de su foto de perfil; 0 si no tiene y se muestran sus iniciales.</param>
/// <param name="Categorias">Categorías activas que entrena en este club, por año; vacía si no entrena.</param>
public record FichaBasicaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    Rol Rol,
    int VersionFoto,
    IReadOnlyList<CategoriaDeFichaBasicaDto> Categorias);
