using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla del saldo y del estado de cuenta de un jugador (spec 015, RF-025;
/// constitución §16.1 y §16.6).
/// Su responsabilidad es calcular lo pendiente de cada cargo y, con todos los cargos del jugador, su
/// saldo y su estado: al día si no debe nada, pago parcial si debe algo y ya se le aplicó parte, debe
/// si debe algo y no se le aplicó nada. Recibe solo lo aplicado por pagos confirmados: quien llama
/// excluye los pagos en revisión, rechazados y anulados.
/// No lee la base de datos, no guarda nada y no restringe ningún acceso (§16.8).
/// </summary>
public static class ReglaEstadoDeCuenta
{
    /// <summary>Un cargo con lo que se le ha aplicado de pagos confirmados.</summary>
    public readonly record struct ValorDeCargo(decimal Valor, decimal Aplicado)
    {
        /// <summary>Lo que falta por pagar de este cargo; nunca negativo.</summary>
        public decimal Pendiente => ReglaEstadoDeCuenta.Pendiente(Valor, Aplicado);
    }

    /// <summary>Saldo y estado de un jugador.</summary>
    public readonly record struct Cuenta(decimal Saldo, EstadoDeCuenta Estado);

    /// <summary>Lo que falta por pagar de un cargo.</summary>
    public static decimal Pendiente(decimal valor, decimal aplicado) => Math.Max(0m, valor - aplicado);

    /// <summary>Saldo y estado a partir de los cargos del jugador con lo aplicado a cada uno.</summary>
    public static Cuenta Calcular(IEnumerable<ValorDeCargo> cargos)
    {
        var saldo = 0m;
        var aplicado = 0m;
        foreach (var cargo in cargos)
        {
            saldo += cargo.Pendiente;
            aplicado += Math.Min(cargo.Aplicado, cargo.Valor);
        }

        return new Cuenta(saldo, Estado(saldo, aplicado));
    }

    /// <summary>Estado de un jugador con ese saldo y esa suma de lo aplicado a sus cargos.</summary>
    public static EstadoDeCuenta Estado(decimal saldo, decimal aplicado) => saldo <= 0m
        ? EstadoDeCuenta.AL_DIA
        : aplicado > 0m ? EstadoDeCuenta.PAGO_PARCIAL : EstadoDeCuenta.DEBE;
}
