namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de a qué cargos y cuánto cubre un pago (spec 015, RF-023 y RF-024).
/// Su responsabilidad es repartir el valor de un pago entre los cargos con algo pendiente: los más
/// antiguos primero, o solo los que el PRESIDENTE eligió, llenando cada uno hasta su pendiente, y
/// avisar cuando no hay nada que cubrir (<c>sin_deuda</c>) o el pago supera lo pendiente
/// (<c>pago_excede_deuda</c>, con lo que se debe).
/// No lee la base de datos, no guarda nada y no sabe de roles.
/// </summary>
public static class ReglaAplicacionDePago
{
    /// <summary>Código cuando el jugador no tiene nada pendiente.</summary>
    public const string SinDeuda = "sin_deuda";

    /// <summary>Código cuando el pago supera lo pendiente.</summary>
    public const string PagoExcedeDeuda = "pago_excede_deuda";

    /// <summary>Un cargo con algo pendiente.</summary>
    /// <param name="CargoId">Identificador del cargo.</param>
    /// <param name="Mes">Mes que cobra; sin mes va al final.</param>
    /// <param name="Pendiente">Lo que falta por pagar.</param>
    public readonly record struct CargoPendiente(Guid CargoId, DateOnly? Mes, decimal Pendiente);

    /// <summary>Lo que un pago cubre de un cargo.</summary>
    public readonly record struct Parte(Guid CargoId, decimal Valor);

    /// <summary>Resultado del reparto: las partes, o el código del rechazo con lo que se debe.</summary>
    public readonly record struct Reparto(IReadOnlyList<Parte> Partes, string? Rechazo, decimal Debe)
    {
        /// <summary>Indica si el pago se pudo repartir.</summary>
        public bool Aplicable => Rechazo is null;
    }

    /// <summary>
    /// Reparte <paramref name="valor"/> entre los cargos pendientes. Si hay <paramref name="cargoIdsElegidos"/>
    /// usa solo esos; en cualquier caso ordena por mes ascendente (RF-024).
    /// </summary>
    public static Reparto Repartir(
        IEnumerable<CargoPendiente> cargosPendientes, decimal valor, IReadOnlyCollection<Guid>? cargoIdsElegidos = null)
    {
        var candidatos = cargosPendientes
            .Where(cargo => cargo.Pendiente > 0m)
            .Where(cargo => cargoIdsElegidos is not { Count: > 0 } || cargoIdsElegidos.Contains(cargo.CargoId))
            .OrderBy(cargo => cargo.Mes ?? DateOnly.MaxValue)
            .ThenBy(cargo => cargo.CargoId)
            .ToList();

        var debe = candidatos.Sum(cargo => cargo.Pendiente);
        if (candidatos.Count == 0)
        {
            return new Reparto([], SinDeuda, 0m);
        }

        if (valor > debe)
        {
            return new Reparto([], PagoExcedeDeuda, debe);
        }

        var partes = new List<Parte>();
        var restante = valor;
        foreach (var cargo in candidatos)
        {
            if (restante <= 0m)
            {
                break;
            }

            var cubierto = Math.Min(restante, cargo.Pendiente);
            partes.Add(new Parte(cargo.CargoId, cubierto));
            restante -= cubierto;
        }

        return new Reparto(partes, null, debe);
    }
}
