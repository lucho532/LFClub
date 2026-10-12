using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de las categorías de un club: la lista, el detalle y su alta,
/// desactivación, reactivación y borrado.
/// Su responsabilidad es recibir la petición y delegar en los servicios.
/// No contiene reglas de negocio: quién consulta y quién cambia lo comprueba
/// <see cref="IntegranteDelClubAttribute"/> en cada acción, y solo el PRESIDENTE cambia algo. No
/// ofrece ninguna operación para cambiar el año de una categoría.
/// </summary>
[Route("api/clubes/{clubId:guid}/categorias")]
[Tags("Categorias")]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorCategorias : ControladorBase
{
    private readonly IServicioConsultaCategorias _consulta;
    private readonly IServicioCategorias _categorias;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ControladorCategorias(IServicioConsultaCategorias consulta, IServicioCategorias categorias)
    {
        _consulta = consulta;
        _categorias = categorias;
    }

    /// <summary>Categorías que le corresponden a quien pregunta, ordenadas por año.</summary>
    [HttpGet]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<IReadOnlyList<CategoriaDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoriaDto>> Listar(CancellationToken cancelacion) =>
        _consulta.ListarAsync(Integrante, cancelacion);

    /// <summary>Crea la categoría de un año de nacimiento.</summary>
    [HttpPost]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<CategoriaConUbicadosDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Crear(CrearCategoriaDto datos, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await _categorias.CrearAsync(datos, cancelacion));

    /// <summary>Una categoría con sus equipos, sus entrenadores y sus jugadores.</summary>
    [HttpGet("{categoriaId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
    public Task<CategoriaDetalleDto> Obtener(Guid categoriaId, CancellationToken cancelacion) =>
        _consulta.ObtenerAsync(categoriaId, Integrante, cancelacion);

    /// <summary>Borra una categoría que nunca ha tenido jugadores ni entrenadores.</summary>
    [HttpDelete("{categoriaId:guid}")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public async Task<IActionResult> Borrar(Guid categoriaId, CancellationToken cancelacion)
    {
        await _categorias.BorrarAsync(categoriaId, cancelacion);
        return NoContent();
    }

    /// <summary>Desactiva una categoría sin jugadores.</summary>
    [HttpPost("{categoriaId:guid}/desactivacion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDto> Desactivar(Guid categoriaId, CancellationToken cancelacion) =>
        _categorias.DesactivarAsync(categoriaId, cancelacion);

    /// <summary>Reactiva una categoría inactiva.</summary>
    [HttpPost("{categoriaId:guid}/reactivacion")]
    [IntegranteDelClub(Rol.PRESIDENTE)]
    [ProducesResponseType<CategoriaConUbicadosDto>(StatusCodes.Status200OK)]
    public Task<CategoriaConUbicadosDto> Reactivar(Guid categoriaId, CancellationToken cancelacion) =>
        _categorias.ReactivarAsync(categoriaId, cancelacion);
}
