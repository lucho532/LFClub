using LFClub.Api.Autorizacion;
using LFClub.Api.Errores;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;
using Microsoft.AspNetCore.Mvc;

namespace LFClub.Api.Controladores.Club;

/// <summary>
/// Representa los endpoints de los entrenadores de una categoría: a quién se puede asignar,
/// asignar, retirar e indicar qué equipos dirige cada uno.
/// Su responsabilidad es recibir la petición y delegar en el servicio.
/// No contiene reglas de negocio: solo admite al PRESIDENTE del club de la ruta, lo que comprueba
/// <see cref="IntegranteDelClubAttribute"/>. No ofrece ninguna operación que cambie el rol de un
/// integrante.
/// </summary>
[Route("api/clubes/{clubId:guid}/categorias/{categoriaId:guid}/entrenadores")]
[Tags("Categorias")]
[IntegranteDelClub(Rol.PRESIDENTE)]
[ProducesResponseType<Problema>(StatusCodes.Status401Unauthorized, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status403Forbidden, Problema.TipoContenido)]
[ProducesResponseType<Problema>(StatusCodes.Status404NotFound, Problema.TipoContenido)]
public class ControladorEntrenadoresDeCategoria : ControladorBase
{
    private readonly IServicioEntrenadoresDeCategoria _servicio;

    /// <summary>Crea el controlador con el servicio de entrenadores de categoría.</summary>
    public ControladorEntrenadoresDeCategoria(IServicioEntrenadoresDeCategoria servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Integrantes que se pueden asignar como entrenadores de la categoría.</summary>
    [HttpGet("candidatos")]
    [ProducesResponseType<IReadOnlyList<CandidatoEntrenadorDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CandidatoEntrenadorDto>> ListarCandidatos(Guid categoriaId, CancellationToken cancelacion) =>
        _servicio.ListarCandidatosAsync(categoriaId, cancelacion);

    /// <summary>Asigna un entrenador a la categoría.</summary>
    [HttpPut("{usuarioRolId:guid}")]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> Asignar(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion) =>
        _servicio.AsignarAsync(categoriaId, usuarioRolId, cancelacion);

    /// <summary>Indica qué equipos de la categoría dirige un entrenador.</summary>
    [HttpPut("{usuarioRolId:guid}/equipos")]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Problema>(StatusCodes.Status400BadRequest, Problema.TipoContenido)]
    [ProducesResponseType<Problema>(StatusCodes.Status409Conflict, Problema.TipoContenido)]
    public Task<CategoriaDetalleDto> ReemplazarEquipos(
        Guid categoriaId, Guid usuarioRolId, EquiposDeEntrenadorDto datos, CancellationToken cancelacion) =>
        _servicio.ReemplazarEquiposAsync(categoriaId, usuarioRolId, datos, cancelacion);

    /// <summary>Retira a un entrenador de la categoría.</summary>
    [HttpDelete("{usuarioRolId:guid}")]
    [ProducesResponseType<CategoriaDetalleDto>(StatusCodes.Status200OK)]
    public Task<CategoriaDetalleDto> Retirar(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion) =>
        _servicio.RetirarAsync(categoriaId, usuarioRolId, cancelacion);
}
