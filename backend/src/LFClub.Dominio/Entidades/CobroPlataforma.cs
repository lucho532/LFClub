using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa lo que un club debe a la plataforma por un mes (spec 017, RF-003 a RF-005).
/// Su responsabilidad es guardar el detalle tal como se generó (integrantes por rol, rango de la
/// tarifa aplicado, total, días de gracia y la fecha de suspensión), que no cambia después (§13): cambiar precios o
/// integrantes no toca un cobro ya generado. Solo cambian el estado y los datos de anulación. Hay a
/// lo sumo un cobro por club y periodo.
/// No implementa <c>IPerteneceAClub</c>: el trabajo en segundo plano y la notificación de la pasarela
/// no tienen club en la petición, así que los repositorios filtran por club de forma explícita
/// (research §6). No guarda el pago, que va en <see cref="PagoPlataforma"/>.
/// </summary>
public class CobroPlataforma
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Club que paga. Borrado en cascada con el club.</summary>
    public Guid ClubId { get; set; }

    /// <summary>Primer día del mes que cobra.</summary>
    public DateOnly Periodo { get; set; }

    /// <summary>Estado del cobro.</summary>
    public EstadoCobroPlataforma Estado { get; set; } = EstadoCobroPlataforma.PENDIENTE;

    /// <summary>Jugadores activos al generar.</summary>
    public int Jugadores { get; set; }

    /// <summary>Entrenadores activos al generar.</summary>
    public int Entrenadores { get; set; }

    /// <summary>Directivos activos al generar.</summary>
    public int Directivos { get; set; }

    /// <summary>Presidentes activos al generar.</summary>
    public int Presidentes { get; set; }

    /// <summary>Precio por jugador vigente al generar.</summary>
    public decimal PrecioJugador { get; set; }

    /// <summary>Precio por entrenador vigente al generar.</summary>
    public decimal PrecioEntrenador { get; set; }

    /// <summary>Precio por directivo vigente al generar.</summary>
    public decimal PrecioDirectivo { get; set; }

    /// <summary>Precio por presidente vigente al generar.</summary>
    public decimal PrecioPresidente { get; set; }

    /// <summary>Primer jugador del rango de la tarifa que se aplicó; nulo en los cobros por precio de rol anteriores.</summary>
    public int? RangoDesde { get; set; }

    /// <summary>Último jugador del rango que se aplicó; nulo si no tenía límite o si no hubo rango.</summary>
    public int? RangoHasta { get; set; }

    /// <summary>Lo que paga el club: el precio fijo de su rango. Siempre mayor que cero.</summary>
    public decimal Total { get; set; }

    /// <summary>Cuándo se generó, en UTC.</summary>
    public DateTime GeneradoEn { get; set; }

    /// <summary>Días de gracia vigentes al generar.</summary>
    public int DiasDeGracia { get; set; }

    /// <summary>
    /// Primer día (en Colombia) en que el club se suspende por este cobro; el último día para pagar
    /// es el anterior.
    /// </summary>
    public DateOnly SuspendeDesde { get; set; }

    /// <summary>
    /// Verdadero si el DESARROLLADOR levantó a mano la suspensión por impago con este cobro vencido:
    /// sigue pendiente y pagable, pero ya no suspende (research §8).
    /// </summary>
    public bool SuspensionOmitida { get; set; }

    /// <summary>Quién anuló el cobro. Solo con estado <c>ANULADO</c>.</summary>
    public Guid? AnuladoPorUsuarioId { get; set; }

    /// <summary>Cuándo se anuló, en UTC.</summary>
    public DateTime? AnuladoEn { get; set; }

    /// <summary>Por qué se anuló (3 a 300 caracteres).</summary>
    public string? MotivoAnulacion { get; set; }
}
