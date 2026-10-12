namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los datos de un club que pueden editar su PRESIDENTE y el DESARROLLADOR (RF-010).
/// Su responsabilidad es llevar el nombre, la sede, la dirección y los datos de contacto.
/// No lleva colores ni escudo: aunque se envíen, se ignoran, porque solo los cambia el DESARROLLADOR desde su panel.
/// </summary>
/// <param name="Nombre">Nombre del club, obligatorio, hasta 120 caracteres.</param>
/// <param name="Sede">Sede, hasta 120 caracteres.</param>
/// <param name="Direccion">Dirección, hasta 200 caracteres.</param>
/// <param name="CorreoContacto">Correo de contacto.</param>
/// <param name="TelefonoContacto">Teléfono de contacto, hasta 20 caracteres.</param>
public record ActualizarConfiguracionClubDto(
    string? Nombre,
    string? Sede,
    string? Direccion,
    string? CorreoContacto,
    string? TelefonoContacto);
