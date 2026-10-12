namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa una categoría o un equipo al que se dirigió un comunicado (spec 011, RF-004 a RF-007).
/// Su responsabilidad es guardar el destino y su nombre copiado en ese momento (§13): con alcance
/// <c>CATEGORIAS</c>, una fila por categoría sin equipo; con <c>EQUIPOS</c>, una fila por equipo,
/// todas de la misma categoría.
/// No guarda quién lo recibió (eso son los destinatarios) y un comunicado con alcance <c>CLUB</c>
/// no tiene ninguna. La coherencia entre el alcance y las filas la garantiza el servicio.
/// </summary>
public class ComunicadoDestino : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Comunicado al que pertenece.</summary>
    public Guid ComunicadoId { get; set; }

    /// <summary>Comunicado al que pertenece.</summary>
    public Comunicado? Comunicado { get; set; }

    /// <summary>Categoría destino. Foránea a <see cref="Categoria"/>; pasa a nulo si se borra.</summary>
    public Guid? CategoriaId { get; set; }

    /// <summary>Nombre de la categoría al publicar, copiado (§13).</summary>
    public string CategoriaNombre { get; set; } = string.Empty;

    /// <summary>Equipo destino; solo con alcance <c>EQUIPOS</c>. A nulo si se borra el equipo.</summary>
    public Guid? EquipoId { get; set; }

    /// <summary>Nombre del equipo al publicar, copiado; solo con alcance <c>EQUIPOS</c>.</summary>
    public string? EquipoNombre { get; set; }
}
