namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa que un entrenamiento va dirigido a una categoría (constitución §11.1).
/// Su responsabilidad es guardar la pareja entrenamiento y categoría.
/// No mueve a ningún jugador de categoría y no se borra aunque la categoría se desactive después:
/// queda en el historial.
/// </summary>
public class EntrenamientoCategoria : IPerteneceAClub
{
    /// <summary>Entrenamiento. Parte de la clave primaria.</summary>
    public Guid EntrenamientoId { get; set; }

    /// <summary>Entrenamiento.</summary>
    public Entrenamiento? Entrenamiento { get; set; }

    /// <summary>Categoría. Parte de la clave primaria.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría.</summary>
    public Categoria? Categoria { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }
}
