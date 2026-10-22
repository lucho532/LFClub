namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa cada vez que el PRESIDENTE inicia un pago de un cobro de la plataforma (spec 017,
/// research §10).
/// Su responsabilidad es guardar la referencia única que viaja a la pasarela y el valor del cobro en
/// ese momento, para asociar después la notificación con el cobro.
/// No prueba ningún pago: sin notificación verificada, no hay pago.
/// </summary>
public class IntentoPagoPlataforma
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Cobro que se intenta pagar. Borrado en cascada.</summary>
    public Guid CobroId { get; set; }

    /// <summary>Referencia única: <c>LFC-</c> y un identificador sin guiones.</summary>
    public string Referencia { get; set; } = string.Empty;

    /// <summary>Valor del cobro al crear el intento, en pesos.</summary>
    public decimal Valor { get; set; }

    /// <summary>Quién lo inició. Nulo si se eliminó esa cuenta.</summary>
    public Guid? CreadoPorUsuarioId { get; set; }

    /// <summary>Cuándo se inició, en UTC.</summary>
    public DateTime CreadoEn { get; set; }
}
