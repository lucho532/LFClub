namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un presidente registrado de un club, tal como lo ve el DESARROLLADOR.
/// Su responsabilidad es identificarlo para poder quitarle el rol.
/// No contiene su documento, su celular ni ningún otro dato personal.
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Correo">Correo de su cuenta.</param>
public record PresidenteDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    string Correo);
