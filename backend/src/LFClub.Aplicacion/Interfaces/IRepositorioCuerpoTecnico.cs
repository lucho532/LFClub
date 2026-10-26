using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Un integrante del cuerpo técnico con lo que sale de su cuenta: correo, celular y versión de la foto.
/// </summary>
/// <param name="Integrante">El integrante del club.</param>
/// <param name="Correo">Correo de su cuenta.</param>
/// <param name="Celular">Celular de su cuenta; nulo si no lo tiene.</param>
/// <param name="VersionFoto">Versión de su foto de perfil; 0 si no tiene.</param>
public record MiembroConContacto(UsuarioRol Integrante, string Correo, string? Celular, int VersionFoto);

/// <summary>
/// Representa la lectura del cuerpo técnico del club de la petición (spec 018).
/// Su responsabilidad es listar a los PRESIDENTE, DIRECTIVO y ENTRENADOR de ingreso aprobado y no retirados,
/// con los datos de su cuenta, y las asignaciones activas de categorías activas con los equipos activos de
/// varios integrantes. Trabaja siempre con el filtro de aislamiento: nunca ve otro club.
/// No modifica datos y no decide quién puede verlo.
/// </summary>
public interface IRepositorioCuerpoTecnico
{
    /// <summary>Todo el cuerpo técnico del club.</summary>
    Task<IReadOnlyList<MiembroConContacto>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Un integrante del cuerpo técnico; nulo si no existe en este club, es JUGADOR o no está aprobado.</summary>
    Task<MiembroConContacto?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Las asignaciones activas de esos integrantes, con su categoría y solo los equipos activos.</summary>
    Task<IReadOnlyList<AsignacionEntrenadorCategoria>> AsignacionesActivasAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);
}
