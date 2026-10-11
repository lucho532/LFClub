using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de consultar partidos (spec 009, RF-010, RF-011, RF-019).
/// Su responsabilidad es responder solo con lo que quien pregunta puede ver, y el mismo 404 para un
/// partido que no ve, que no existe o que es de otro club.
/// No modifica datos ni conoce HTTP.
/// </summary>
public interface IServicioConsultaPartidos
{
    /// <summary>
    /// Los partidos visibles, opcionalmente de un estado o una categoría: los próximos por fecha
    /// ascendente y después los jugados y cancelados, por fecha descendente.
    /// </summary>
    Task<IReadOnlyList<PartidoResumenDto>> ListarAsync(
        EstadoPartido? estado, Guid? categoriaId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>El detalle de un partido visible.</summary>
    Task<PartidoDto> ObtenerAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
