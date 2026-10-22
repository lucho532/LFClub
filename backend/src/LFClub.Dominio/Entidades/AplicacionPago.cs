namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa qué parte de un pago cubre un cargo (spec 015, RF-024).
/// Su responsabilidad es guardar el valor que un pago confirmado aplica a un cargo. Se crea al
/// confirmar y no cambia: si el pago se anula siguen ahí y el cálculo del saldo las ignora.
/// No decide el reparto (lo hace <c>ReglaAplicacionDePago</c>) ni valida el tope del cargo.
/// </summary>
public class AplicacionPago : IPerteneceAClub
{
    /// <summary>Pago. Parte de la clave primaria; borrado en cascada.</summary>
    public Guid PagoId { get; set; }

    /// <summary>Cargo cubierto. Parte de la clave primaria; borrado en cascada.</summary>
    public Guid CargoId { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Lo que cubre de ese cargo. Mayor que cero.</summary>
    public decimal Valor { get; set; }
}
