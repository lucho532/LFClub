namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla del mes de cobro (spec 015, RF-008 y research §3).
/// Su responsabilidad es decir cuál es el mes en curso en Colombia y desde qué mes se le cobra a quien
/// entra, es aprobado o se reincorpora: el mes siguiente, sin cargos retroactivos. Usa el desfase fijo
/// de UTC−5 porque Colombia no tiene horario de verano, así que no depende de la base de zonas
/// horarias del contenedor.
/// No genera cargos ni lee el reloj: recibe el instante.
/// </summary>
public static class ReglaMesDeCobro
{
    private static readonly TimeSpan DesfaseDeColombia = TimeSpan.FromHours(-5);

    /// <summary>Primer día del mes que corre en Colombia en ese instante UTC.</summary>
    public static DateOnly MesEnCurso(DateTime ahoraUtc)
    {
        var local = DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc) + DesfaseDeColombia;
        return new DateOnly(local.Year, local.Month, 1);
    }

    /// <summary>Primer día del mes siguiente al de Colombia en ese instante: desde cuándo se cobra.</summary>
    public static DateOnly PrimerMesDeCobro(DateTime ahoraUtc) => MesEnCurso(ahoraUtc).AddMonths(1);
}
