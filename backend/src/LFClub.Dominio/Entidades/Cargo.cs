using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa lo que un jugador debe al club por un concepto (spec 015, RF-009 a RF-013).
/// Su responsabilidad es guardar el valor cobrado, el mes que cobra y la categoría del jugador cuando
/// se generó (§13). Se crea una vez, por la generación mensual, y no cambia: cambiar después el valor
/// de la mensualidad no toca los cargos ya generados (RF-013). Hay a lo sumo una mensualidad por
/// jugador y mes. Se borra en cascada con el integrante.
/// No guarda lo pagado ni el saldo, que se calculan de las aplicaciones de pagos confirmados.
/// </summary>
public class Cargo : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Jugador que debe. Foránea a <see cref="UsuarioRol"/>, borrado en cascada.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Tipo de cargo; esta funcionalidad solo crea <c>MENSUALIDAD</c>.</summary>
    public TipoCargo Tipo { get; set; }

    /// <summary>Primer día del mes que cobra. Obligatorio en <c>MENSUALIDAD</c>.</summary>
    public DateOnly? Mes { get; set; }

    /// <summary>Lo cobrado, en pesos enteros. Mayor que cero.</summary>
    public decimal Valor { get; set; }

    /// <summary>Categoría del jugador cuando se generó; pasa a nulo si se borra la categoría (§13).</summary>
    public Guid? CategoriaId { get; set; }

    /// <summary>Fecha y hora de generación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }
}
