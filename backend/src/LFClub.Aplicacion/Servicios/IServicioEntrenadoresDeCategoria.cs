using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso con los que el PRESIDENTE decide quién entrena cada categoría
/// (constitución §8 y §11.3; RF-017 a RF-021 y RF-028).
/// Su responsabilidad es ofrecer a quién se puede asignar, asignar y retirar a un integrante de una
/// categoría e indicar qué equipos de ella dirige.
/// No cambia el rol de nadie: un PRESIDENTE o un DIRECTIVO asignado conserva su único rol y su
/// alcance (RF-017a). No comprueba el rol de quien llama: lo hace la autorización.
/// </summary>
public interface IServicioEntrenadoresDeCategoria
{
    /// <summary>
    /// Integrantes aprobados del club con rol ENTRENADOR, DIRECTIVO o PRESIDENTE que todavía no
    /// están asignados a la categoría, por apellidos.
    /// </summary>
    Task<IReadOnlyList<CandidatoEntrenadorDto>> ListarCandidatosAsync(
        Guid categoriaId, CancellationToken cancelacion = default);

    /// <summary>Asigna al integrante a la categoría; si ya lo estaba, no cambia nada.</summary>
    Task<CategoriaDetalleDto> AsignarAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Retira al integrante de la categoría, con lo que deja de dirigir sus equipos; si no estaba
    /// asignado, no cambia nada.
    /// </summary>
    Task<CategoriaDetalleDto> RetirarAsync(Guid categoriaId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Reemplaza la lista completa de equipos de la categoría que dirige el integrante.</summary>
    Task<CategoriaDetalleDto> ReemplazarEquiposAsync(
        Guid categoriaId, Guid usuarioRolId, EquiposDeEntrenadorDto datos, CancellationToken cancelacion = default);
}
