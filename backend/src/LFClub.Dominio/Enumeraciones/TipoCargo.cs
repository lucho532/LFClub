namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el tipo de un cargo (spec 015, RF-009).
/// Su responsabilidad es distinguir la mensualidad que la aplicación genera sola de los cargos de
/// otro concepto, que esta funcionalidad todavía no crea.
/// No guarda el valor ni el mes del cargo.
/// </summary>
public enum TipoCargo
{
    /// <summary>Mensualidad de un mes; la genera la aplicación, una por jugador y mes.</summary>
    MENSUALIDAD,

    /// <summary>Cargo de otro concepto; reservado, aún sin operación que lo cree.</summary>
    OTRO,
}
