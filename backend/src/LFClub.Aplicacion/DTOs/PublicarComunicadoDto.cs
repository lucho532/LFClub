using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que envía el equipo del club para publicar un comunicado (spec 011, RF-001 a
/// RF-010).
/// Su responsabilidad es llevar la clave de publicación que generó la pantalla, el título, el texto
/// plano, el alcance y, según este, las categorías o los equipos destino.
/// No valida nada: lo hacen <c>ValidadorPublicarComunicado</c>, la regla de audiencia y el servicio.
/// </summary>
/// <param name="Clave">Clave de la publicación; la repetida por el mismo integrante no crea otro comunicado.</param>
/// <param name="Titulo">Título, hasta 100 caracteres.</param>
/// <param name="Texto">Texto plano, hasta 1.000 caracteres.</param>
/// <param name="Alcance">A quién va dirigido.</param>
/// <param name="CategoriaIds">Categorías destino; solo con <c>CATEGORIAS</c>.</param>
/// <param name="EquipoIds">Equipos destino, de una sola categoría; solo con <c>EQUIPOS</c>.</param>
public record PublicarComunicadoDto(
    Guid Clave,
    string Titulo,
    string Texto,
    AlcanceComunicado Alcance,
    IReadOnlyList<Guid>? CategoriaIds,
    IReadOnlyList<Guid>? EquipoIds);
