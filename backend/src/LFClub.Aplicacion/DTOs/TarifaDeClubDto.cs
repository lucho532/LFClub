namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que le corresponde pagar a un club según la tarifa vigente (spec 017).
/// Su responsabilidad es mostrar, en la pantalla de tarifas, el club, sus jugadores activos, el rango
/// que le toca y el valor mensual. Es una vista previa: el cobro real se genera al empezar el mes.
/// No lleva datos personales, solo números.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Jugadores">Jugadores activos del club.</param>
/// <param name="Rango">Rango que le corresponde; nulo si ninguno cubre esa cantidad.</param>
/// <param name="ValorMensual">Lo que pagaría al mes; 0 si no le corresponde ningún cobro.</param>
/// <param name="MesDeInicioDelCobro">Mes <c>AAAA-MM</c> desde el que se le cobra; nulo si no se le cobra.</param>
public record TarifaDeClubDto(
    Guid ClubId,
    string Nombre,
    int Jugadores,
    RangoTarifaDto? Rango,
    decimal ValorMensual,
    string? MesDeInicioDelCobro);
