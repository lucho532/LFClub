using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al crear o cambiar un partido (spec 009, RF-007).
/// Su responsabilidad es llevar los datos tal como llegan, en hora de Colombia: fecha
/// <c>AAAA-MM-DD</c> y hora <c>HH:mm</c>.
/// No valida nada: eso lo hace <c>ValidadorPartido</c>. Al cambiar, sus categorías deben ser las que
/// ya tiene.
/// </summary>
/// <param name="Rival">Equipo rival, de 120 caracteres como máximo.</param>
/// <param name="Fecha">Fecha del partido.</param>
/// <param name="Hora">Hora de inicio.</param>
/// <param name="Lugar">Lugar, de 120 caracteres como máximo.</param>
/// <param name="Condicion">Local o visitante.</param>
/// <param name="Tipo">Amistoso o de torneo.</param>
/// <param name="NombreTorneo">Obligatorio si es de torneo; se ignora en un amistoso.</param>
/// <param name="CategoriaIds">Una o más categorías, sin repetir.</param>
public record GuardarPartidoDto(
    string? Rival,
    string? Fecha,
    string? Hora,
    string? Lugar,
    CondicionPartido? Condicion,
    TipoPartido? Tipo,
    string? NombreTorneo,
    IReadOnlyList<Guid>? CategoriaIds);
