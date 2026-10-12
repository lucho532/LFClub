using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que ubica a mano a un jugador en una categoría.
/// Su responsabilidad es comprobar en el servidor que el integrante es un jugador del club (rol
/// JUGADOR, aprobado y no retirado) y que la categoría de destino es de su club y está activa, y
/// cambiarlo en una transacción con el club bloqueado, de modo que entre dos cambios simultáneos
/// valga el último y el jugador quede en una sola categoría (RF-013). Si venía de otra categoría,
/// sale de todos sus equipos (RF-027).
/// No accede al contexto de Entity Framework ni conoce HTTP, y no toca ningún otro dato del jugador.
/// </summary>
public class ServicioUbicacionJugador : IServicioUbicacionJugador
{
    private readonly IRepositorioClub _club;
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioJugadores _jugadores;
    private readonly LectorDeCategorias _lector;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioUbicacionJugador(
        IRepositorioClub club,
        IRepositorioCategorias categorias,
        IRepositorioJugadores jugadores,
        LectorDeCategorias lector,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _club = club;
        _categorias = categorias;
        _jugadores = jugadores;
        _lector = lector;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public Task<CategoriaDetalleDto> UbicarAsync(
        Guid usuarioRolId, UbicarJugadorDto datos, CancellationToken cancelacion = default)
    {
        var errores = new ErroresDeValidacion();
        if (datos.CategoriaId is null)
        {
            errores.Agregar("categoriaId", "Elige la categoría.");
        }

        errores.LanzarSiHayErrores();
        var categoriaId = datos.CategoriaId!.Value;

        return _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var jugador = await _jugadores.ObtenerAsync(usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                var categoria = await _categorias.ObtenerAsync(categoriaId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                if (!jugador.EsJugadorDelClub)
                {
                    throw ErroresDeCategorias.NoEsJugador();
                }

                if (!categoria.Activa)
                {
                    throw ErroresDeCategorias.CategoriaInactiva();
                }

                // Ubicarlo donde ya está no tiene efecto y conserva sus equipos.
                if (jugador.CategoriaId != categoriaId)
                {
                    await _jugadores.SacarDeSusEquiposAsync(usuarioRolId, cancelacion);
                    await _jugadores.CambiarCategoriaAsync(usuarioRolId, categoriaId, cancelacion);
                    await _categorias.MarcarUsadaAsync(categoriaId, cancelacion);
                }

                return await _lector.DetalleAsync(categoriaId, conDocumentacion: true, cancelacion);
            },
            cancelacion);
    }
}
