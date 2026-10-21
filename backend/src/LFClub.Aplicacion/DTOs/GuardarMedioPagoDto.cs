namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el PRESIDENTE para crear o editar un medio de pago (spec 015, RF-027).
/// Su responsabilidad es llevar la entidad, el titular, el número, la nota y si está activo.
/// No valida nada: lo hace <c>ValidadorMedioPago</c>.
/// </summary>
/// <param name="Entidad">Hasta 40 caracteres; obligatoria.</param>
/// <param name="Titular">Hasta 100 caracteres; obligatorio.</param>
/// <param name="Numero">Hasta 60 caracteres; obligatorio.</param>
/// <param name="Nota">Hasta 200 caracteres; opcional.</param>
/// <param name="Activo">Si se muestra a las familias.</param>
public record GuardarMedioPagoDto(string? Entidad, string? Titular, string? Numero, string? Nota, bool Activo);
