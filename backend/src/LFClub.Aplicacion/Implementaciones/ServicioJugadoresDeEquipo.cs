using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que pone y saca jugadores de los equipos de una categoría.
/// Su responsabilidad es comprobar en el servidor que el equipo es activo y de la categoría actual
/// del jugador, y hacerlo con el club bloqueado, para que un cambio de categoría simultáneo no deje
/// nunca a un jugador en un equipo de una categoría que no es la suya (RF-025).
/// No accede al contexto de Entity Framework ni conoce HTTP, y no cambia la categoría de nadie.
/// </summary>
public class ServicioJugadoresDeEquipo : IServicioJugadoresDeEquipo
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioEquipos _equipos;
    private readonly IRepositorioJugadores _jugadores;
    private readonly LectorDeCategorias _lector;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioJugadoresDeEquipo(
        IRepositorioClub club,
        IRepositorioEquipos equipos,
        IRepositorioJugadores jugadores,
        LectorDeCategorias lector,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _club = club;
        _equipos = equipos;
        _jugadores = jugadores;
        _lector = lector;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> PonerAsync(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await ComprobarQueExistenAsync(categoriaId, equipoId, usuarioRolId, cancelacion);

                var jugador = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion);
                if (jugador is not { EsJugadorDelClub: true } || jugador.CategoriaId != categoriaId)
                {
                    throw ErroresDeCategorias.JugadorDeOtraCategoria();
                }

                await _equipos.PonerJugadorAsync(equipoId, usuarioRolId, cancelacion);
                await _equipos.MarcarUsadosAsync([equipoId], cancelacion);
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> SacarAsync(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                await ComprobarQueExistenAsync(categoriaId, equipoId, usuarioRolId, cancelacion);

                await _equipos.SacarJugadorAsync(equipoId, usuarioRolId, cancelacion);
                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);

    /// <summary>El equipo activo de esa categoría y el integrante deben existir en el club: si no, 404.</summary>
    private async Task ComprobarQueExistenAsync(
        Guid categoriaId, Guid equipoId, Guid usuarioRolId, CancellationToken cancelacion)
    {
        if (await _equipos.ObtenerActivoAsync(categoriaId, equipoId, cancelacion) is null
            || await _jugadores.ObtenerAsync(usuarioRolId, cancelacion) is null)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }
    }
}
