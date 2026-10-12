using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la tarjeta "Mi categoría" de la familia.
/// Su responsabilidad es leer el integrante de quien pregunta con sus equipos y la categoría que
/// tiene, con la cuenta de cada entrenador para la versión de su foto, y convertirlos en el DTO de
/// la familia, que es un tipo distinto del que ve el club (constitución §23): de cada entrenador
/// lleva solo su identificador, su nombre, sus equipos y la versión de su foto (RF-014 de la 008).
/// No lee ninguna otra categoría ni ningún otro jugador, no modifica datos y no accede al contexto
/// de Entity Framework.
/// </summary>
public class ServicioMiCategoria : IServicioMiCategoria
{
    private readonly IRepositorioJugadores _jugadores;
    private readonly IRepositorioCategorias _categorias;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioMiCategoria(IRepositorioJugadores jugadores, IRepositorioCategorias categorias)
    {
        _jugadores = jugadores;
        _categorias = categorias;
    }

    /// <inheritdoc />
    public async Task<MiCategoriaDto> ObtenerAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var jugador = await _jugadores.ObtenerConEquiposAsync(quienPregunta.Id, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        var categoria = jugador.CategoriaId is { } categoriaId
            ? await _categorias.ObtenerConCuentasDeEntrenadoresAsync(categoriaId, cancelacion)
            : null;

        return MapperCategorias.AMiCategoria(jugador, categoria);
    }
}
