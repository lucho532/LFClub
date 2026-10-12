using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de consultas del apartado "Categorías".
/// Su responsabilidad es leer las categorías del club de la petición y dejar pasar solo las que
/// <see cref="ReglaAlcanceDeCategorias"/> permite a quien pregunta: para un ENTRENADOR, las activas
/// en las que tiene una asignación activa, que se consulta en cada petición. A las listas de
/// jugadores les añade el estado de su documentación, que solo reciben el PRESIDENTE y los
/// DIRECTIVOS (RF-032 de la 005), y a la de retirados, el enlace a la ficha básica de quien retiró
/// a cada uno si sigue en el club (spec 008).
/// No modifica datos, no accede al contexto de Entity Framework y no recibe un identificador de
/// club: solo existe el que fijó la autorización.
/// </summary>
public class ServicioConsultaCategorias : IServicioConsultaCategorias
{
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioAsignaciones _asignaciones;
    private readonly IRepositorioJugadores _jugadores;
    private readonly LectorDeCategorias _lector;
    private readonly ResolutorDeEnlaces _enlaces;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaCategorias(
        IRepositorioCategorias categorias,
        IRepositorioAsignaciones asignaciones,
        IRepositorioJugadores jugadores,
        LectorDeCategorias lector,
        ResolutorDeEnlaces enlaces)
    {
        _categorias = categorias;
        _asignaciones = asignaciones;
        _jugadores = jugadores;
        _lector = lector;
        _enlaces = enlaces;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(
        UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var categorias = await _categorias.ListarAsync(cancelacion);
        if (!ReglaAlcanceDeCategorias.VeTodas(quienPregunta.Rol))
        {
            var suyas = (await _asignaciones.CategoriasDeAsync(quienPregunta.Id, cancelacion)).ToHashSet();
            categorias = categorias
                .Where(categoria => ReglaAlcanceDeCategorias.PuedeVer(
                    quienPregunta.Rol, categoria.Activa, suyas.Contains(categoria.Id)))
                .ToList();
        }

        var jugadores = await _categorias.ContarJugadoresAsync(cancelacion);
        return categorias
            .Select(categoria => MapperCategorias.ACategoria(categoria, jugadores.GetValueOrDefault(categoria.Id)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CategoriaDetalleDto> ObtenerAsync(
        Guid categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        if (!ReglaAlcanceDeCategorias.VeTodas(quienPregunta.Rol))
        {
            var categoria = await _categorias.ObtenerAsync(categoriaId, cancelacion);
            var asignada = categoria is not null
                && await _asignaciones.TieneActivaAsync(categoriaId, quienPregunta.Id, cancelacion);

            // El mismo 404 si no existe, si está inactiva o si no es suya: no se revela cuál (§17.2).
            if (categoria is null || !ReglaAlcanceDeCategorias.PuedeVer(quienPregunta.Rol, categoria.Activa, asignada))
            {
                throw ExcepcionDeAplicacion.NoEncontrado();
            }
        }

        // El estado de la documentación de cada jugador es solo para quien ve las listas del club:
        // un ENTRENADOR recibe el detalle de su categoría sin él (RF-007 de la 005).
        return await _lector.DetalleAsync(
            categoriaId, ReglaAlcanceDeCategorias.VeListasDelClub(quienPregunta.Rol), cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorDeCategoriaDto>> ListarSinCategoriaAsync(CancellationToken cancelacion = default)
    {
        var jugadores = await _jugadores.ListarSinCategoriaAsync(cancelacion);
        var entregados = await _lector.EntregadosAsync(jugadores.Select(jugador => jugador.Id), cancelacion);

        return jugadores
            .PorApellidos()
            .Select(jugador => MapperCategorias.AJugador(
                jugador, null, MapperCategorias.DocumentosPendientes(entregados, jugador.Id)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorRetiradoDto>> ListarRetiradosAsync(CancellationToken cancelacion = default)
    {
        var jugadores = await _jugadores.ListarRetiradosAsync(cancelacion);
        var entregados = await _lector.EntregadosAsync(jugadores.Select(jugador => jugador.Id), cancelacion);
        var autores = await _enlaces.AutoresAsync(jugadores.Select(jugador => jugador.RetiradoPorUsuarioId), cancelacion);

        return jugadores
            .Select(jugador => MapperCategorias.ARetirado(
                jugador,
                MapperCategorias.DocumentosPendientes(entregados, jugador.Id),
                jugador.RetiradoPorUsuarioId is { } autorId ? autores.GetValueOrDefault(autorId) : null))
            .ToList();
    }
}
