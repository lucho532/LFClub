namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el borrado masivo que necesita eliminar una cuenta (spec 014, constitución §12.4 y
/// §7.1 por la excepción de la propia cuenta).
/// Su responsabilidad es ejecutar, sin seguimiento y sobre todos los clubes de esa cuenta, los tres
/// pasos que la cascada no hace sola, en este orden: <see cref="AnonimizarAutoriaAsync"/>,
/// <see cref="AnularInvitacionesAsync"/> y <see cref="EliminarCuentaYSusIntegrantesAsync"/>.
/// No decide reglas (presidente único, contraseña): las comprueba el servicio antes, y debe llamarse
/// dentro de la misma transacción.
/// </summary>
public interface IRepositorioEliminacionCuenta
{
    /// <summary>
    /// Escribe "Persona eliminada" en los nombres copiados de lo que la cuenta o sus integrantes
    /// publicaron, cambiaron o aprobaron (§13), salvo en las filas que se borrarán en el mismo paso.
    /// </summary>
    Task AnonimizarAutoriaAsync(
        Guid usuarioId, IReadOnlyCollection<Guid> integranteIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Anula las invitaciones que la cuenta envió y nadie usó ni anuló todavía; las demás no cambian.
    /// </summary>
    Task AnularInvitacionesAsync(Guid usuarioId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra la convocatoria, los goles y la asistencia de los integrantes, luego los integrantes
    /// (la cascada limpia lo que cuelga de ellos) y por último la cuenta. Deja el seguimiento vacío.
    /// </summary>
    Task EliminarCuentaYSusIntegrantesAsync(
        Guid usuarioId, IReadOnlyCollection<Guid> integranteIds, CancellationToken cancelacion = default);
}
