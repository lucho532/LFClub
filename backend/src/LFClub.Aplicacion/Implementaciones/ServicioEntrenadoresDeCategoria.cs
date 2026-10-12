using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que asigna y retira entrenadores de una categoría e indica qué equipos
/// dirigen.
/// Su responsabilidad es comprobar en el servidor, contra la fila del integrante, que es asignable
/// según <see cref="ReglaEntrenadorAsignable"/> y que la categoría está activa, y ejecutar cada
/// cambio en una transacción con el club bloqueado. Como todas las consultas van con el filtro del
/// club, un integrante, una categoría o un equipo de otro club no existen: 404.
/// No toca el rol del integrante, no borra asignaciones (RF-021) y no accede al contexto de Entity
/// Framework ni conoce HTTP.
/// </summary>
public class ServicioEntrenadoresDeCategoria : IServicioEntrenadoresDeCategoria
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioAsignaciones _asignaciones;
    private readonly IRepositorioJugadores _integrantes;
    private readonly IRepositorioEquipos _equipos;
    private readonly LectorDeCategorias _lector;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEntrenadoresDeCategoria(
        IRepositorioClub club,
        IRepositorioCategorias categorias,
        IRepositorioAsignaciones asignaciones,
        IRepositorioJugadores integrantes,
        IRepositorioEquipos equipos,
        LectorDeCategorias lector,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _club = club;
        _categorias = categorias;
        _asignaciones = asignaciones;
        _integrantes = integrantes;
        _equipos = equipos;
        _lector = lector;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CandidatoEntrenadorDto>> ListarCandidatosAsync(
        Guid categoriaId, CancellationToken cancelacion = default)
    {
        var categoria = await CategoriaAsync(categoriaId, cancelacion);
        var yaAsignados = categoria.Asignaciones
            .Where(asignacion => asignacion.Activa)
            .Select(asignacion => asignacion.UsuarioRolId)
            .ToHashSet();

        return (await _asignaciones.ListarIntegrantesAprobadosAsync(cancelacion))
            .Where(integrante => ReglaEntrenadorAsignable.EsAsignable(integrante.Rol, integrante.EstadoIngreso)
                && !yaAsignados.Contains(integrante.Id))
            .PorApellidos()
            .Select(MapperCategorias.ACandidato)
            .ToList();
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> AsignarAsync(
        Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var categoria = await CategoriaAsync(categoriaId, cancelacion);
                var integrante = await IntegranteAsync(usuarioRolId, cancelacion);

                if (!ReglaEntrenadorAsignable.EsAsignable(integrante.Rol, integrante.EstadoIngreso))
                {
                    throw ErroresDeCategorias.NoAsignableComoEntrenador();
                }

                if (!categoria.Activa)
                {
                    throw ErroresDeCategorias.CategoriaInactiva();
                }

                await _asignaciones.AsignarAsync(categoriaId, usuarioRolId, _reloj.AhoraUtc, cancelacion);
                await _categorias.MarcarUsadaAsync(categoriaId, cancelacion);
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> RetirarAsync(
        Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await CategoriaAsync(categoriaId, cancelacion);
                await IntegranteAsync(usuarioRolId, cancelacion);

                await _asignaciones.RetirarAsync(categoriaId, usuarioRolId, cancelacion);
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> ReemplazarEquiposAsync(
        Guid categoriaId, Guid usuarioRolId, EquiposDeEntrenadorDto datos, CancellationToken cancelacion = default)
    {
        var errores = new ErroresDeValidacion();
        if (datos.EquipoIds is null)
        {
            errores.Agregar("equipoIds", "Indica qué equipos dirige; la lista puede ir vacía.");
        }

        errores.LanzarSiHayErrores();
        var equipoIds = datos.EquipoIds!.Distinct().ToList();

        return _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await CategoriaAsync(categoriaId, cancelacion);
                await IntegranteAsync(usuarioRolId, cancelacion);

                // Un equipo inactivo o de otra categoría no existe en esta: 404 y no cambia nada.
                var activos = await _equipos.ListarActivosAsync(categoriaId, cancelacion);
                if (equipoIds.Except(activos).Any())
                {
                    throw ExcepcionDeAplicacion.NoEncontrado();
                }

                var asignacion = await _asignaciones.ObtenerAsync(categoriaId, usuarioRolId, cancelacion);
                if (asignacion is not { Activa: true })
                {
                    throw ErroresDeCategorias.EntrenadorNoAsignado();
                }

                await _asignaciones.ReemplazarEquiposAsync(asignacion.Id, equipoIds, cancelacion);
                await _equipos.MarcarUsadosAsync(equipoIds, cancelacion);
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);
    }

    private async Task<Categoria> CategoriaAsync(Guid categoriaId, CancellationToken cancelacion) =>
        await _categorias.ObtenerAsync(categoriaId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();

    private async Task<UsuarioRol> IntegranteAsync(Guid usuarioRolId, CancellationToken cancelacion) =>
        await _integrantes.ObtenerAsync(usuarioRolId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
}
