using LFClub.Dominio.Reglas;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa la configuración de cobro de la plataforma (spec 017, RF-001 y RF-002).
/// Su responsabilidad es guardar los rangos de la tarifa (precio fijo según la cantidad de jugadores)
/// y los días de gracia vigentes. Hay una sola fila (<c>Id = 1</c>). Cambiarla solo afecta a los cobros que se generen después.
/// No es de ningún club y no guarda llaves ni secretos de la pasarela, que viven en la configuración
/// de la aplicación.
/// </summary>
public class ConfiguracionPlataforma
{
    /// <summary>Clave primaria; siempre 1.</summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Rangos de la tarifa: según cuántos jugadores activos tiene el club, paga el precio fijo de su
    /// rango. Van ordenados y seguidos, empiezan en 1 y el último no tiene límite superior. Vacío (lo inicial) = no se cobra.
    /// </summary>
    public List<RangoTarifa> Rangos { get; set; } = [];

    /// <summary>Días que el club puede tardar en pagar antes de suspenderse. De 0 a 30; por defecto 5.</summary>
    public int DiasDeGracia { get; set; } = 5;

    /// <summary>Quién hizo el último cambio. Nulo si se eliminó esa cuenta.</summary>
    public Guid? ActualizadoPorUsuarioId { get; set; }

    /// <summary>Cuándo se hizo el último cambio, en UTC.</summary>
    public DateTime ActualizadoEn { get; set; }
}
