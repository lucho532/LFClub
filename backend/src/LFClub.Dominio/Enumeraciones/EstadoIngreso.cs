namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado de ingreso de un integrante en su club (constitución §12.1.1).
/// Su responsabilidad es distinguir a quien espera aprobación de quien ya está dentro del club.
/// Quien entra con una invitación nace aprobado; solo espera el jugador agregado desde la ficha de
/// un hermano.
/// No describe el estado del club ni el de la cuenta.
/// </summary>
public enum EstadoIngreso
{
    /// <summary>El ingreso está pendiente de aprobación.</summary>
    EN_ESPERA,

    /// <summary>
    /// El integrante entra a su club: entró con una invitación o el PRESIDENTE aprobó su ingreso.
    /// </summary>
    APROBADO,
}
