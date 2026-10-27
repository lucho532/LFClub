namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la tarifa vigente de la plataforma tal como la ve el PRESIDENTE (spec 017).
/// Su responsabilidad es explicarle cómo se le cobra: los rangos de jugadores con su precio mensual fijo, los
/// días de gracia, cuántos jugadores activos tiene su club hoy y en qué rango cae.
/// Es informativa y de solo lectura: el valor del cobro real se congela al generarlo. No lleva datos de otros
/// clubes ni llaves de la pasarela.
/// </summary>
/// <param name="Rangos">Rangos de la tarifa, de menos a más jugadores; vacía si todavía no hay tarifa definida.</param>
/// <param name="DiasDeGracia">Días que el club puede tardar en pagar antes de suspenderse.</param>
/// <param name="Jugadores">Jugadores activos del club hoy.</param>
/// <param name="RangoDelClub">Rango en el que cae el club hoy; nulo si ninguno cubre esa cantidad.</param>
public record TarifaVigenteDto(
    IReadOnlyList<RangoTarifaDto> Rangos,
    int DiasDeGracia,
    int Jugadores,
    RangoTarifaDto? RangoDelClub);
