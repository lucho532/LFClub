namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa la configuración de cobro de la plataforma (spec 017, RF-001 y RF-002).
/// Su responsabilidad es guardar los cuatro precios por rol y los días de gracia vigentes. Hay una
/// sola fila (<c>Id = 1</c>). Cambiarla solo afecta a los cobros que se generen después.
/// No es de ningún club y no guarda llaves ni secretos de la pasarela, que viven en la configuración
/// de la aplicación.
/// </summary>
public class ConfiguracionPlataforma
{
    /// <summary>Clave primaria; siempre 1.</summary>
    public int Id { get; set; } = 1;

    /// <summary>Precio mensual por jugador, en pesos enteros. Nulo = sin definir (cuenta como 0).</summary>
    public decimal? PrecioJugador { get; set; }

    /// <summary>Precio mensual por entrenador. Nulo = sin definir.</summary>
    public decimal? PrecioEntrenador { get; set; }

    /// <summary>Precio mensual por directivo. Nulo = sin definir.</summary>
    public decimal? PrecioDirectivo { get; set; }

    /// <summary>Precio mensual por presidente. Nulo = sin definir.</summary>
    public decimal? PrecioPresidente { get; set; }

    /// <summary>Días que el club puede tardar en pagar antes de suspenderse. De 0 a 30; por defecto 5.</summary>
    public int DiasDeGracia { get; set; } = 5;

    /// <summary>Quién hizo el último cambio. Nulo si se eliminó esa cuenta.</summary>
    public Guid? ActualizadoPorUsuarioId { get; set; }

    /// <summary>Cuándo se hizo el último cambio, en UTC.</summary>
    public DateTime ActualizadoEn { get; set; }
}
