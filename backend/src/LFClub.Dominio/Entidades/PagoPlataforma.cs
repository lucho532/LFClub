using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa el pago confirmado de un cobro de la plataforma (spec 017, RF-014).
/// Su responsabilidad es guardar el comprobante: valor, fecha, origen y, si vino de la pasarela, la
/// referencia, la transacción y el medio de pago. Hay a lo sumo uno por cobro.
/// Nunca se edita ni se borra (§13), salvo con el club. No guarda datos de tarjetas ni cuentas.
/// </summary>
public class PagoPlataforma
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Cobro que paga. Único; borrado en cascada.</summary>
    public Guid CobroId { get; set; }

    /// <summary>Lo pagado, en pesos; igual al total del cobro.</summary>
    public decimal Valor { get; set; }

    /// <summary>Cuándo se confirmó, en UTC.</summary>
    public DateTime PagadoEn { get; set; }

    /// <summary>De dónde viene el pago.</summary>
    public OrigenPagoPlataforma Origen { get; set; }

    /// <summary>Referencia del intento. Solo en <c>PASARELA</c>.</summary>
    public string? Referencia { get; set; }

    /// <summary>Identificador de la transacción en la pasarela. Solo en <c>PASARELA</c>.</summary>
    public string? TransaccionId { get; set; }

    /// <summary>Medio informado por la pasarela (PSE, NEQUI, CARD…).</summary>
    public string? MedioDePago { get; set; }

    /// <summary>Quién lo marcó. Solo en <c>MANUAL</c>; nulo si se eliminó esa cuenta.</summary>
    public Guid? RegistradoPorUsuarioId { get; set; }
}
