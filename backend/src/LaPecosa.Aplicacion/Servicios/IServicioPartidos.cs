using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de crear, cambiar, cancelar y reactivar partidos (spec 009,
/// historia 2).
/// Su responsabilidad es que solo los gestione el PRESIDENTE o quien entrena todas sus categorías,
/// con categorías activas del club, y que cada cambio respete el estado y el momento del partido.
/// Todas responden con el detalle tal como lo ve quien lo pidió.
/// No conoce HTTP, no toca la convocatoria y nunca borra un partido.
/// </summary>
public interface IServicioPartidos
{
    /// <summary>Crea un partido próximo, sin convocados.</summary>
    Task<PartidoDto> CrearAsync(GuardarPartidoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Cambia los datos de un partido próximo; las categorías no cambian.</summary>
    Task<PartidoDto> CambiarAsync(
        Guid partidoId, GuardarPartidoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Corrige el rival de un partido ya jugado, que no admite otro cambio de datos; el marcador y los
    /// goles no se tocan.
    /// </summary>
    Task<PartidoDto> CorregirRivalAsync(
        Guid partidoId, CorregirRivalDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Cancela un partido próximo.</summary>
    Task<PartidoDto> CancelarAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Reactiva un partido cancelado que todavía no empieza, con su convocatoria como estaba.</summary>
    Task<PartidoDto> ReactivarAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
