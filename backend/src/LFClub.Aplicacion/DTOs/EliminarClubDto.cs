namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la confirmación expresa para eliminar un club (constitución §7.4, RF-029).
/// Su responsabilidad es llevar el nombre del club escrito por quien lo elimina.
/// No basta con el identificador: sin el nombre exacto no se elimina nada.
/// </summary>
/// <param name="NombreDeConfirmacion">Debe coincidir con el nombre del club.</param>
public record EliminarClubDto(
    string? NombreDeConfirmacion);
