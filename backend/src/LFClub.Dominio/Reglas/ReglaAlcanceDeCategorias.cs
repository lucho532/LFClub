using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de qué categorías ve cada rol dentro de su club (constitución §7.5 y §12.3;
/// RF-031, RF-034 y RF-035).
/// Su responsabilidad es resolver el alcance a partir del rol, que es lo único que decide: el
/// PRESIDENTE y el DIRECTIVO ven todas las categorías, activas e inactivas, y las listas "Sin
/// categoría" y "Retirados"; el ENTRENADOR, solo las activas en las que tiene una asignación
/// activa y ninguna de las dos listas; el JUGADOR, ninguna.
/// No da más alcance a un PRESIDENTE o a un DIRECTIVO por estar asignado como entrenador, ni se lo
/// quita (RF-017a). No consulta las asignaciones ni conoce HTTP.
/// </summary>
public static class ReglaAlcanceDeCategorias
{
    /// <summary>Indica si ese rol ve todas las categorías del club, activas e inactivas.</summary>
    public static bool VeTodas(Rol rol) => rol is Rol.PRESIDENTE or Rol.DIRECTIVO;

    /// <summary>Indica si ese rol ve las listas "Sin categoría" y "Retirados".</summary>
    public static bool VeListasDelClub(Rol rol) => VeTodas(rol);

    /// <summary>
    /// Indica si un integrante con ese rol ve una categoría, dado si está activa y si él tiene en
    /// ella una asignación activa.
    /// </summary>
    public static bool PuedeVer(Rol rol, bool categoriaActiva, bool tieneAsignacionActiva) => rol switch
    {
        Rol.PRESIDENTE or Rol.DIRECTIVO => true,
        Rol.ENTRENADOR => categoriaActiva && tieneAsignacionActiva,
        _ => false,
    };
}
