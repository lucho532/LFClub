using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de a quién se puede asignar como entrenador de una categoría (constitución
/// §8; RF-017).
/// Su responsabilidad es responder si un integrante es asignable: tiene el ingreso aprobado y su
/// rol es ENTRENADOR, DIRECTIVO o PRESIDENTE. Nadie más: ni un JUGADOR ni quien sigue en espera.
/// No comprueba que el integrante sea del club de la categoría ni que la categoría esté activa, y
/// no cambia el rol de nadie: quedar asignado no da ni quita nada (RF-017a).
/// </summary>
public static class ReglaEntrenadorAsignable
{
    private static readonly Rol[] RolesAsignables = [Rol.ENTRENADOR, Rol.DIRECTIVO, Rol.PRESIDENTE];

    /// <summary>Indica si un integrante con ese rol y ese estado de ingreso puede entrenar una categoría.</summary>
    public static bool EsAsignable(Rol rol, EstadoIngreso estadoIngreso) =>
        estadoIngreso == EstadoIngreso.APROBADO && RolesAsignables.Contains(rol);
}
