using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using static LFClub.Dominio.Reglas.ReglaAplicacionDePago;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el paso de confirmar un pago, que comparten el registro directo del PRESIDENTE y la
/// confirmación de un pago en revisión (spec 015, RF-020, RF-022 a RF-025; research §7).
/// Su responsabilidad es, con el jugador ya bloqueado: leer sus cargos pendientes, repartir el valor con
/// <see cref="ReglaAplicacionDePago"/> (rechazando con <c>pago_excede_deuda</c> o <c>sin_deuda</c>), dar
/// el siguiente número de recibo del club, escribir el concepto, calcular lo que le queda debiendo y
/// dejar el pago confirmado con quién lo hizo, cuándo y con sus aplicaciones.
/// No abre la transacción, no bloquea al jugador, no valida el importe, no comprueba el rol y no guarda:
/// quien lo llama lo hace todo antes y después.
/// </summary>
public class AplicadorDePagos
{
    private const int LongitudMaximaDelConcepto = 300;

    private readonly IRepositorioCargos _cargos;
    private readonly IRepositorioPagos _pagos;

    /// <summary>Crea el aplicador con sus dependencias.</summary>
    public AplicadorDePagos(IRepositorioCargos cargos, IRepositorioPagos pagos)
    {
        _cargos = cargos;
        _pagos = pagos;
    }

    /// <summary>
    /// Confirma el pago por ese valor, aplicándolo a los cargos elegidos o a los más antiguos. Deja
    /// <c>Estado</c>, <c>Valor</c>, <c>Concepto</c>, <c>NumeroRecibo</c>, <c>SaldoTrasPago</c>, quién y
    /// cuándo confirmó, y las aplicaciones; no toca sus demás datos.
    /// </summary>
    public async Task ConfirmarAsync(
        Pago pago,
        decimal valor,
        IReadOnlyCollection<Guid>? cargoIds,
        UsuarioRol quienConfirma,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var cargos = (await _cargos.ListarConAplicadoAsync(pago.UsuarioRolId, cancelacion))
            .Where(cargo => ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado) > 0m)
            .ToList();
        var pendientes = cargos
            .Select(cargo => new CargoPendiente(
                cargo.CargoId, cargo.Mes, ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado)))
            .ToList();

        var reparto = Repartir(pendientes, valor, cargoIds);
        if (!reparto.Aplicable)
        {
            throw reparto.Rechazo == SinDeuda
                ? ErroresDePagos.SinDeuda()
                : ErroresDePagos.PagoExcedeDeuda(reparto.Debe);
        }

        var debeTodo = pendientes.Sum(cargo => cargo.Pendiente);
        var nombre = $"{quienConfirma.Nombres} {quienConfirma.Apellidos}";

        pago.Estado = EstadoPago.CONFIRMADO;
        pago.Valor = valor;
        pago.Concepto = Concepto(reparto.Partes.Select(parte => cargos.Single(cargo => cargo.CargoId == parte.CargoId)));
        pago.NumeroRecibo = await _pagos.SiguienteReciboAsync(pago.ClubId, cancelacion);
        pago.SaldoTrasPago = debeTodo - valor;
        pago.ConfirmadoEn = ahoraUtc;
        pago.ConfirmadoPorUsuarioId = quienConfirma.UsuarioId;
        pago.ConfirmadoPorNombre = nombre;
        foreach (var parte in reparto.Partes)
        {
            pago.Aplicaciones.Add(new AplicacionPago { CargoId = parte.CargoId, ClubId = pago.ClubId, Valor = parte.Valor });
        }
    }

    /// <summary>"Mensualidad octubre de 2026; Mensualidad noviembre de 2026", recortado si no cabe.</summary>
    private static string Concepto(IEnumerable<CargoConAplicado> cubiertos)
    {
        var texto = string.Join("; ", cubiertos.OrderBy(cargo => cargo.Mes).Select(cargo => TextoDelCargo.De(cargo.Tipo, cargo.Mes)));
        return texto.Length <= LongitudMaximaDelConcepto ? texto : texto[..(LongitudMaximaDelConcepto - 1)] + "…";
    }
}

/// <summary>
/// Representa el texto con el que se nombra un cargo en un concepto de pago.
/// Su responsabilidad es escribir "Mensualidad octubre de 2026" con el mes en español.
/// No usa la cultura del servidor: los nombres de los meses van fijos (<see cref="TextoDePesos.Meses"/>).
/// </summary>
public static class TextoDelCargo
{
    /// <summary>El texto del cargo: su tipo y, si tiene, su mes.</summary>
    public static string De(TipoCargo tipo, DateOnly? mes)
    {
        var nombre = tipo == TipoCargo.MENSUALIDAD ? "Mensualidad" : "Otro cargo";
        return mes is { } primerDia ? $"{nombre} {TextoDePesos.Meses[primerDia.Month - 1]} de {primerDia.Year}" : nombre;
    }
}
