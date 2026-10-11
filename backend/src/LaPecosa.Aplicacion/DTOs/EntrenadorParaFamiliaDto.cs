namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que una familia ve de un entrenador de la categoría de su jugador (RF-035 de la
/// 003 y RF-014 de la 008).
/// Su responsabilidad es llevar su identificador en el club, para abrir su ficha básica, su nombre,
/// sus apellidos, los nombres de los equipos que dirige y la versión de su foto de perfil, para
/// mostrar su miniatura. La parte de RF-035 que prohibía el identificador queda sin efecto.
/// No lleva rol, correo, celular ni documento: es un tipo distinto del que ve el club (constitución
/// §23).
/// </summary>
/// <param name="UsuarioRolId">Integrante del club; con él se abre su ficha básica.</param>
/// <param name="Nombres">Nombres del entrenador.</param>
/// <param name="Apellidos">Apellidos del entrenador.</param>
/// <param name="Equipos">Nombres de los equipos que dirige; vacía si lo es de la categoría en general.</param>
/// <param name="VersionFoto">Versión de su foto de perfil; 0 si no tiene y se muestran sus iniciales.</param>
public record EntrenadorParaFamiliaDto(
    Guid UsuarioRolId, string Nombres, string Apellidos, IReadOnlyList<string> Equipos, int VersionFoto);
