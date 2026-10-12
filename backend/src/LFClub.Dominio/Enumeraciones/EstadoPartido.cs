namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado actual de un partido (constitución §18; spec 009, RF-014, RF-019 y RF-021).
/// Su responsabilidad es distinguir el partido que todavía no se juega, el cancelado y el jugado,
/// porque de eso depende quién lo ve (§17.2).
/// No guarda historial de estados: las transiciones admitidas las decide
/// <c>ReglaTransicionPartido</c>.
/// </summary>
public enum EstadoPartido
{
    /// <summary>Todavía sin resultado: solo lo ven sus convocados, quienes lo entrenan y el PRESIDENTE.</summary>
    PROXIMO,

    /// <summary>Cancelado: lo siguen viendo, marcado, los mismos que lo veían.</summary>
    CANCELADO,

    /// <summary>Con resultado registrado: lo ve todo el club.</summary>
    JUGADO,
}
