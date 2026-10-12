namespace LaPecosa.Dominio.Enumeraciones;

/// <summary>
/// Representa el tipo de un partido (spec 009, RF-007).
/// Su responsabilidad es distinguir el amistoso del partido de un torneo, que además lleva el
/// nombre del torneo.
/// No representa torneos con tablas, fechas o equipos: eso está fuera de alcance.
/// </summary>
public enum TipoPartido
{
    /// <summary>Partido amistoso; no lleva nombre de torneo.</summary>
    AMISTOSO,

    /// <summary>Partido de un torneo; exige su nombre.</summary>
    TORNEO,
}
