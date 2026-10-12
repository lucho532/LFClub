namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se hace con un presidente al quitarle el rol.
/// Su responsabilidad es nombrar las dos únicas opciones del contrato.
/// No contiene el rol nuevo: va aparte en <see cref="RetirarPresidenteDto"/>.
/// </summary>
public enum AccionRetiroPresidente
{
    /// <summary>Sigue en el club con otro rol.</summary>
    ASIGNAR_ROL,

    /// <summary>Deja de pertenecer al club; su integrante se borra.</summary>
    ELIMINAR_DEL_CLUB,
}
