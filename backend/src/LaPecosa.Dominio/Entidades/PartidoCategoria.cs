namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa que un partido es de una categoría (constitución §11.1).
/// Su responsabilidad es guardar la pareja partido y categoría.
/// No mueve a ningún jugador de categoría y no se borra aunque la categoría se desactive después.
/// </summary>
public class PartidoCategoria : IPerteneceAClub
{
    /// <summary>Partido. Parte de la clave primaria.</summary>
    public Guid PartidoId { get; set; }

    /// <summary>Partido.</summary>
    public Partido? Partido { get; set; }

    /// <summary>Categoría. Parte de la clave primaria.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría.</summary>
    public Categoria? Categoria { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }
}
