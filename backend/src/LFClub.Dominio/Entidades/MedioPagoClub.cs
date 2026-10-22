namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa una cuenta por la que el club recibe pagos (spec 015, RF-026 a RF-028).
/// Su responsabilidad es guardar la entidad, el titular, el número y una nota que el PRESIDENTE
/// publica y que las familias ven al pagar. Desactivarla la oculta a las familias sin borrarla.
/// No guarda dinero ni toca los pagos hechos con esa entidad: cada pago guarda el nombre de la
/// entidad como texto.
/// </summary>
public class MedioPagoClub : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Nequi, Bancolombia, Daviplata u otra. Máx. 40. Obligatoria.</summary>
    public string Entidad { get; set; } = string.Empty;

    /// <summary>Nombre del titular. Máx. 100. Obligatorio.</summary>
    public string Titular { get; set; } = string.Empty;

    /// <summary>Número de cuenta, celular o llave. Máx. 60. Obligatorio.</summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>Por ejemplo, "Ahorros". Máx. 200. Opcional.</summary>
    public string? Nota { get; set; }

    /// <summary>Falso significa que no se muestra a las familias.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Fecha y hora de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }
}
