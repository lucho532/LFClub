namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato que comparten la aprobación y el rechazo de un ingreso.
/// Su responsabilidad es que ambos casos de uso respondan con el mismo código y el mismo texto.
/// No decide cuándo se produce cada error.
/// </summary>
public static class ErroresDeIngreso
{
    /// <summary>
    /// 404: ese integrante no existe en el club de la petición, o su ingreso ya fue rechazado. El
    /// texto es el mismo en los dos casos y para un identificador de otro club.
    /// </summary>
    public static ExcepcionDeAplicacion NoEncontrado() => new(
        "no_encontrado", 404, "Esa persona ya no está en la sala de espera de este club.");

    /// <summary>
    /// 403: la aprobación indica un rol distinto de JUGADOR. Al aprobar no se elige rol; la persona
    /// sigue en espera (RF-016).
    /// </summary>
    public static ExcepcionDeAplicacion RolNoAsignable() => new(
        "rol_no_asignable",
        403,
        "Al aprobar un ingreso no se elige rol: la persona entra siempre como jugador.");

    /// <summary>409: el ingreso ya no está en espera porque alguien lo aprobó antes.</summary>
    public static ExcepcionDeAplicacion YaAprobado() => ExcepcionDeAplicacion.Conflicto(
        "ingreso_ya_aprobado", "Ese ingreso ya estaba aprobado. No se cambió nada.");
}
