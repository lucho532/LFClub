using System.Globalization;
using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa lo que comparten los mappers de entrenamientos, partidos y estadísticas (spec 009).
/// Su responsabilidad es escribir una hora como <c>HH:mm</c> y una categoría como
/// <see cref="CategoriaDeActividadDto"/>.
/// No decide qué categorías o qué datos ve quien pregunta.
/// </summary>
public static class MapperActividades
{
    /// <summary>La hora como <c>HH:mm</c>.</summary>
    public static string Hora(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Una categoría con su año; cero si no se cargó.</summary>
    public static CategoriaDeActividadDto Categoria(Guid categoriaId, Categoria? categoria) =>
        new(categoriaId, categoria?.Anio ?? 0);

    /// <summary>Las categorías de una actividad, por año.</summary>
    public static IReadOnlyList<CategoriaDeActividadDto> Categorias(IEnumerable<(Guid Id, Categoria? Categoria)> categorias) =>
        categorias.Select(par => Categoria(par.Id, par.Categoria)).OrderBy(categoria => categoria.Anio).ToList();
}
