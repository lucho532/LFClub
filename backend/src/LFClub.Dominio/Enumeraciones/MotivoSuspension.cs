namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa por qué un club está suspendido (spec 017, RF-018).
/// Su responsabilidad es distinguir la suspensión que decide el DESARROLLADOR de la que aplica el
/// sistema por falta de pago, porque solo la segunda se levanta con un pago.
/// No dice si el club está suspendido: eso lo dice <see cref="EstadoClub"/>.
/// </summary>
public enum MotivoSuspension
{
    /// <summary>La puso el DESARROLLADOR a mano; un pago nunca la levanta.</summary>
    MANUAL,

    /// <summary>La puso el sistema porque venció el plazo de pago; se levanta al confirmarse el pago.</summary>
    IMPAGO,
}
