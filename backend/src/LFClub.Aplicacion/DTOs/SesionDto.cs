namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la cuenta con sesión y los clubes a los que pertenece.
/// Su responsabilidad es decir quién es la persona y entre qué clubes puede elegir.
/// No contiene la contraseña, su hash ni ningún token (RF-019).
/// </summary>
/// <param name="UsuarioId">Identificador de la cuenta.</param>
/// <param name="Correo">Correo de la cuenta.</param>
/// <param name="EsDesarrollador">Indica si es la cuenta DESARROLLADOR.</param>
/// <param name="VersionFoto">0 si no hay foto de perfil; cambia cada vez que se reemplaza.</param>
/// <param name="Clubes">Clubes de la cuenta; vacío para el DESARROLLADOR.</param>
public record SesionDto(
    Guid UsuarioId,
    string Correo,
    bool EsDesarrollador,
    int VersionFoto,
    IReadOnlyList<ClubDeSesionDto> Clubes);
