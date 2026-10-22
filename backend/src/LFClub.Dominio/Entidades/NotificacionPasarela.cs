using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa una notificación recibida de la pasarela de pago (spec 017, RF-012 y RF-015).
/// Su responsabilidad es dejar constancia de lo recibido y de lo que se hizo con ello, y hacer la
/// notificación idempotente con el índice único de transacción y estado.
/// No guarda el cuerpo completo ni la firma, solo los campos de este registro.
/// </summary>
public class NotificacionPasarela
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Cuándo llegó, en UTC.</summary>
    public DateTime RecibidaEn { get; set; }

    /// <summary>Qué pasó con ella.</summary>
    public ResultadoNotificacion Resultado { get; set; }

    /// <summary>Identificador de la transacción. Nulo si la firma no era válida.</summary>
    public string? TransaccionId { get; set; }

    /// <summary>Estado informado por la pasarela (APPROVED, DECLINED…).</summary>
    public string? EstadoPasarela { get; set; }

    /// <summary>Referencia del intento.</summary>
    public string? Referencia { get; set; }

    /// <summary>Valor informado, en pesos.</summary>
    public decimal? Valor { get; set; }

    /// <summary>Cobro asociado, cuando se pudo asociar. Borrado en cascada.</summary>
    public Guid? CobroId { get; set; }
}
