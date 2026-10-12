namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa una categoría o un equipo al que se dirigió un comunicado (spec 011, RF-024).
/// Su responsabilidad es llevar los nombres copiados al publicar y los identificadores de hoy, que
/// son nulos si la categoría o el equipo se borró.
/// No lo recibe nunca la cuenta de un jugador (RF-032).
/// </summary>
/// <param name="CategoriaId">Categoría; nula si se borró.</param>
/// <param name="CategoriaNombre">Nombre de la categoría al publicar.</param>
/// <param name="EquipoId">Equipo; nulo con alcance <c>CATEGORIAS</c> o si se borró.</param>
/// <param name="EquipoNombre">Nombre del equipo al publicar; nulo con alcance <c>CATEGORIAS</c>.</param>
public record DestinoDeComunicadoDto(Guid? CategoriaId, string CategoriaNombre, Guid? EquipoId, string? EquipoNombre);
