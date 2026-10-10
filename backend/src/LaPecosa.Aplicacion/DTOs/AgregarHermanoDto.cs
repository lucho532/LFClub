using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa los datos con los que la familia agrega un hermano desde la ficha de un jugador ya
/// registrado (constitución §12.1.2; RF-002 de la 006).
/// Su responsabilidad es llevar la identidad del nuevo jugador y, solo cuando hace falta, el
/// nombre del responsable.
/// No lleva correo, celular ni contraseña: son los de la cuenta, que el hermano comparte. Tampoco
/// lleva el club ni la cuenta, que son los del jugador desde cuya ficha se agrega.
/// </summary>
/// <param name="Nombres">Nombres, hasta 80 caracteres.</param>
/// <param name="Apellidos">Apellidos, hasta 80 caracteres.</param>
/// <param name="TipoDocumento">Tipo de documento.</param>
/// <param name="NumeroDocumento">Número de documento, hasta 20 caracteres; se guarda sin espacios ni puntos.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento; no puede ser futura.</param>
/// <param name="NombreResponsable">
/// Nombre del padre, madre o responsable, hasta 160 caracteres. Obligatorio solo si el hermano es
/// menor de 18 años y la cuenta no tiene responsable; si la cuenta ya lo tiene, se ignora.
/// </param>
public record AgregarHermanoDto(
    string? Nombres,
    string? Apellidos,
    TipoDocumento? TipoDocumento,
    string? NumeroDocumento,
    DateOnly? FechaNacimiento,
    string? NombreResponsable);
