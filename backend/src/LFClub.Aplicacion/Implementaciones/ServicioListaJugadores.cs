using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la lista "Jugadores".
/// Su responsabilidad es leer los jugadores activos del club de la petición y dejar pasar solo los
/// que quien pregunta puede ver con <see cref="ReglaAlcanceDeCategorias"/>: quien no ve las listas
/// del club (el ENTRENADOR) recibe solo los de las categorías activas en las que tiene una
/// asignación activa, que se consulta en cada petición, y nunca a los que no tienen categoría.
/// Después completa la fecha de la foto de todos con una sola consulta y los agrupa con
/// <see cref="MapperListaJugadores"/>.
/// No modifica datos, no crea ninguna regla de alcance propia, no accede al contexto de Entity
/// Framework y no recibe un identificador de club: solo existe el que fijó la autorización.
/// </summary>
public class ServicioListaJugadores : IServicioListaJugadores
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioAsignaciones _asignaciones;
    private readonly IRepositorioFotosJugador _fotos;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioListaJugadores(
        IRepositorioJugadores jugadores, IRepositorioAsignaciones asignaciones, IRepositorioFotosJugador fotos)
    {
        _jugadores = jugadores;
        _asignaciones = asignaciones;
        _fotos = fotos;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GrupoDeJugadoresDto>> ListarAsync(
        UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var jugadores = await _jugadores.ListarDelClubAsync(cancelacion);
        if (!ReglaAlcanceDeCategorias.VeListasDelClub(quienPregunta.Rol))
        {
            // La asignación se consulta en cada petición: quien la pierde deja de ver al instante (CE-002).
            var suyas = (await _asignaciones.CategoriasDeAsync(quienPregunta.Id, cancelacion)).ToHashSet();
            jugadores = jugadores
                .Where(jugador => jugador.Categoria is { } categoria
                    && ReglaAlcanceDeCategorias.PuedeVer(quienPregunta.Rol, categoria.Activa, suyas.Contains(categoria.Id)))
                .ToList();
        }

        var fotos = await _fotos.FechasAsync(jugadores.Select(jugador => jugador.Id).ToList(), cancelacion);
        return MapperListaJugadores.AGrupos(jugadores, fotos);
    }
}
