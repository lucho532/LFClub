namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado del aviso fijo de mora del inicio de la familia (spec 016, RF-006 a RF-008a;
/// constitución §16.7).
/// Su responsabilidad es resumir en tres valores si el jugador debe una mensualidad cuyo plazo ya
/// pasó, si solo la tiene pendiente dentro del plazo o si no debe nada.
/// No se guarda: lo calcula <c>ReglaMora</c> y no restringe ningún acceso.
/// </summary>
public enum EstadoDeAvisoDeMora
{
    /// <summary>No tiene ninguna mensualidad pendiente.</summary>
    AL_DIA,

    /// <summary>Tiene la mensualidad del mes pendiente, pero todavía no pasa el día 5.</summary>
    PENDIENTE_EN_PLAZO,

    /// <summary>Tiene una mensualidad sin pagar cuyo plazo (día 5 de su mes) ya pasó.</summary>
    EN_MORA,
}
