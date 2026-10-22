namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el DESARROLLADOR para fijar los precios y los días de gracia (spec 017,
/// RF-001 y RF-002).
/// Su responsabilidad es llevar los valores; cada precio es nulo o un entero de pesos no negativo.
/// No valida nada: lo hace <c>ServicioTarifas</c>.
/// </summary>
/// <param name="PrecioJugador">Precio mensual por jugador.</param>
/// <param name="PrecioEntrenador">Precio mensual por entrenador.</param>
/// <param name="PrecioDirectivo">Precio mensual por directivo.</param>
/// <param name="PrecioPresidente">Precio mensual por presidente.</param>
/// <param name="DiasDeGracia">Días de gracia, de 0 a 30.</param>
public record GuardarTarifasDto(
    decimal? PrecioJugador,
    decimal? PrecioEntrenador,
    decimal? PrecioDirectivo,
    decimal? PrecioPresidente,
    int DiasDeGracia);
