using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de la mora de un jugador (spec 016, RF-001 a RF-005; constitución §16.7).
/// Su responsabilidad es decidir el estado del aviso fijo del inicio con los cargos del jugador y la
/// fecha de Colombia: la mensualidad de un mes tiene plazo hasta el día 5 de ese mes, inclusive, y
/// desde el día 6 sin pagar deja al jugador en mora. Solo cuentan las mensualidades con algo
/// pendiente; los cargos OTRO no tienen plazo y no cuentan. Recibe solo lo aplicado por pagos
/// confirmados no anulados.
/// No lee el reloj ni la base de datos, no guarda nada y no restringe ningún acceso.
/// </summary>
public static class ReglaMora
{
    /// <summary>Último día del mes, inclusive, en que se paga la mensualidad (§16.7).</summary>
    public const int UltimoDiaDelPlazo = 5;

    /// <summary>Un cargo del jugador con lo que se le ha aplicado de pagos confirmados.</summary>
    public readonly record struct CargoParaMora(TipoCargo Tipo, DateOnly? Mes, decimal Valor, decimal Aplicado);

    /// <summary>
    /// El estado del aviso con sus cifras: lo vencido, lo pendiente dentro del plazo y el día en que
    /// vence esto último.
    /// </summary>
    public readonly record struct Mora(
        EstadoDeAvisoDeMora Estado,
        decimal ValorVencido,
        int MesesVencidos,
        decimal ValorEnPlazo,
        DateOnly? VenceEl);

    /// <summary>Último día del plazo de la mensualidad del mes que empieza en <paramref name="mes"/>.</summary>
    public static DateOnly VencimientoDe(DateOnly mes) => new(mes.Year, mes.Month, UltimoDiaDelPlazo);

    /// <summary>El estado de mora con esos cargos, el día de hoy en Colombia.</summary>
    public static Mora Calcular(IEnumerable<CargoParaMora> cargos, DateTime ahoraUtc)
    {
        var hoy = ReglaMesDeCobro.DiaEnColombia(ahoraUtc);
        var valorVencido = 0m;
        var mesesVencidos = 0;
        var valorEnPlazo = 0m;
        DateOnly? venceEl = null;

        foreach (var cargo in cargos)
        {
            var pendiente = ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado);
            if (cargo.Tipo != TipoCargo.MENSUALIDAD || cargo.Mes is not { } mes || pendiente <= 0m)
            {
                continue;
            }

            var vencimiento = VencimientoDe(mes);
            if (hoy > vencimiento)
            {
                valorVencido += pendiente;
                mesesVencidos++;
            }
            else
            {
                valorEnPlazo += pendiente;
                venceEl = venceEl is null || vencimiento < venceEl ? vencimiento : venceEl;
            }
        }

        var estado = mesesVencidos > 0
            ? EstadoDeAvisoDeMora.EN_MORA
            : valorEnPlazo > 0m ? EstadoDeAvisoDeMora.PENDIENTE_EN_PLAZO : EstadoDeAvisoDeMora.AL_DIA;

        return new Mora(estado, valorVencido, mesesVencidos, valorEnPlazo, venceEl);
    }
}
