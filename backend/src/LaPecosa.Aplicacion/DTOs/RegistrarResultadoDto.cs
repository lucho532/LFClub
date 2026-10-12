namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al registrar o corregir el resultado de un partido (spec 009, RF-016
/// y RF-017).
/// Su responsabilidad es llevar el marcador y, de cada convocado, si jugó y sus goles. Los
/// convocados que no se envían quedan como que no jugaron y sin goles.
/// No valida nada: lo hacen el servicio y <c>ReglaResultadoDePartido</c>.
/// </summary>
/// <param name="GolesClub">Goles del club, de 0 a 99.</param>
/// <param name="GolesRival">Goles del rival, de 0 a 99.</param>
/// <param name="Jugadores">Cada convocado con lo que jugó.</param>
public record RegistrarResultadoDto(int? GolesClub, int? GolesRival, IReadOnlyList<JugadorDelResultadoDto>? Jugadores);

/// <summary>
/// Representa a un convocado en el resultado que se registra.
/// Su responsabilidad es decir si jugó y cuántos goles marcó.
/// No lleva su categoría, que es la copiada al convocarlo.
/// </summary>
/// <param name="UsuarioRolId">Jugador convocado.</param>
/// <param name="Jugo">Si jugó.</param>
/// <param name="Goles">Goles que marcó; solo si jugó.</param>
public record JugadorDelResultadoDto(Guid UsuarioRolId, bool Jugo, int Goles);
