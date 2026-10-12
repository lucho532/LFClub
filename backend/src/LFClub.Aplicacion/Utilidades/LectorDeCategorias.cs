using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la lectura de una categoría tal como la devuelve la API, que comparten la consulta
/// y todas las operaciones que responden con la categoría después de cambiarla.
/// Su responsabilidad es cargar la categoría del club de la petición con sus equipos, sus
/// entrenadores y, en el detalle, sus jugadores, y convertirla en DTO. Cuando quien llama lo pide,
/// añade a cada jugador el estado de su documentación, con una sola consulta para toda la lista y
/// sin leer el contenido de ningún archivo (RF-032 de la 005).
/// No comprueba el alcance de quien pregunta ni decide si puede ver la documentación: eso lo hace
/// cada servicio antes de llamarlo. No cambia nada.
/// </summary>
public class LectorDeCategorias
{
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioDocumentosJugador _documentos;

    /// <summary>Crea el lector con sus dependencias.</summary>
    public LectorDeCategorias(
        IRepositorioCategorias categorias, IRepositorioJugadores jugadores, IRepositorioDocumentosJugador documentos)
    {
        _categorias = categorias;
        _jugadores = jugadores;
        _documentos = documentos;
    }

    /// <summary>La categoría sin la lista de sus jugadores; 404 si no existe en el club.</summary>
    public async Task<CategoriaDto> ResumenAsync(Guid categoriaId, CancellationToken cancelacion = default)
    {
        var categoria = await _categorias.ObtenerAsync(categoriaId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        return MapperCategorias.ACategoria(categoria, await _categorias.ContarJugadoresAsync(categoriaId, cancelacion));
    }

    /// <summary>
    /// La categoría con sus jugadores; 404 si no existe en el club.
    /// <paramref name="conDocumentacion"/> indica si cada jugador lleva cuántos documentos le
    /// faltan: solo debe ser verdadero para un PRESIDENTE o un DIRECTIVO (RF-007 de la 005).
    /// </summary>
    public async Task<CategoriaDetalleDto> DetalleAsync(
        Guid categoriaId, bool conDocumentacion, CancellationToken cancelacion = default)
    {
        var categoria = await _categorias.ObtenerAsync(categoriaId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var jugadores = await _jugadores.ListarDeCategoriaAsync(categoriaId, cancelacion);

        return MapperCategorias.ADetalle(
            categoria, jugadores, conDocumentacion ? await EntregadosAsync(jugadores.Select(j => j.Id), cancelacion) : null);
    }

    /// <summary>Cuántos documentos tiene entregados cada uno de esos jugadores, en una sola consulta.</summary>
    public Task<IReadOnlyDictionary<Guid, int>> EntregadosAsync(
        IEnumerable<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
        _documentos.ContarEntregadosAsync(usuarioRolIds.ToList(), cancelacion);
}
