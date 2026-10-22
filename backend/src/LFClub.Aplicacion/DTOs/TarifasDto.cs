namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa los precios y los días de gracia del cobro de la plataforma (spec 017, RF-001).
/// Su responsabilidad es llevar lo que el DESARROLLADOR ve en la pantalla de tarifas y si la pasarela
/// de pago está configurada.
/// No lleva llaves ni secretos de la pasarela, solo si están o no.
/// </summary>
/// <param name="PrecioJugador">Precio mensual por jugador; nulo si no está definido.</param>
/// <param name="PrecioEntrenador">Precio mensual por entrenador; nulo si no está definido.</param>
/// <param name="PrecioDirectivo">Precio mensual por directivo; nulo si no está definido.</param>
/// <param name="PrecioPresidente">Precio mensual por presidente; nulo si no está definido.</param>
/// <param name="DiasDeGracia">Días de gracia antes de suspender, de 0 a 30.</param>
/// <param name="PasarelaConfigurada">Verdadero si la configuración de la pasarela está completa.</param>
/// <param name="ActualizadoEn">Cuándo se guardó por última vez; nulo si nunca.</param>
public record TarifasDto(
    decimal? PrecioJugador,
    decimal? PrecioEntrenador,
    decimal? PrecioDirectivo,
    decimal? PrecioPresidente,
    int DiasDeGracia,
    bool PasarelaConfigurada,
    DateTime? ActualizadoEn);
