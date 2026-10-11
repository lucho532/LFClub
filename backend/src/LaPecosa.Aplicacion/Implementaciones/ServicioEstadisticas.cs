using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de las estadísticas deportivas (spec 009, RF-026 a RF-030).
/// Su responsabilidad es pasar por <see cref="AccesoAFicha"/> de la 005 antes de dar las cifras de
/// un jugador (quien no ve su ficha recibe el mismo 404, RF-028) y pedir al repositorio los conteos
/// y los goleadores, que el mapper convierte. Una categoría de otro club o inexistente responde 404.
/// No guarda estadísticas, no modifica datos y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioEstadisticas : IServicioEstadisticas
{
    private readonly AccesoAFicha _ficha;
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioEstadisticas _estadisticas;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEstadisticas(AccesoAFicha ficha, AccesoAActividad acceso, IRepositorioEstadisticas estadisticas)
    {
        _ficha = ficha;
        _acceso = acceso;
        _estadisticas = estadisticas;
    }

    /// <inheritdoc />
    public async Task<EstadisticasJugadorDto> DelJugadorAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await _ficha.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);
        var porCategoria = await _estadisticas.DelJugadorAsync(usuarioRolId, cancelacion);
        return MapperEstadisticas.AEstadisticas(usuarioRolId, porCategoria);
    }

    /// <inheritdoc />
    public async Task<GoleadoresDto> GoleadoresAsync(
        Guid? categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var conGoles = await _estadisticas.CategoriasConGolesAsync(cancelacion);
        if (categoriaId is not { } elegidaId)
        {
            return MapperEstadisticas.AGoleadores(conGoles, null, [], await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion));
        }

        var elegida = await _estadisticas.CategoriaAsync(elegidaId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var goleadores = await _estadisticas.GoleadoresAsync(elegidaId, cancelacion);
        var quien = await _acceso.QuienPreguntaAsync(quienPregunta, cancelacion);
        return MapperEstadisticas.AGoleadores(conGoles, elegida, goleadores, quien);
    }
}
