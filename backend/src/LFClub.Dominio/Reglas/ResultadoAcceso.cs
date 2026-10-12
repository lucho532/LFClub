namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa el resultado de evaluar el acceso de un integrante a su club.
/// Su responsabilidad es distinguir el acceso permitido de los cuatro motivos de rechazo.
/// No contiene el mensaje ni el código de error que ve la persona.
/// </summary>
public enum ResultadoAcceso
{
    /// <summary>El integrante puede entrar.</summary>
    Permitido,

    /// <summary>El club está suspendido y el integrante no es su presidente.</summary>
    ClubSuspendido,

    /// <summary>El club está dado de baja: no entra nadie.</summary>
    ClubDadoDeBaja,

    /// <summary>El club le dejaría entrar, pero su ingreso sigue pendiente de aprobación.</summary>
    IngresoEnEspera,

    /// <summary>El club le dejaría entrar, pero lo retiró: ya no está en él (§14.1).</summary>
    IntegranteRetirado,
}
