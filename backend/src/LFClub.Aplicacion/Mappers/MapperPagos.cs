using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de cargos y pagos en los DTOs de la cuenta de un jugador (spec 015).
/// Su responsabilidad es calcular lo pendiente de cada cargo con <see cref="ReglaEstadoDeCuenta"/>,
/// decidir si un pago tiene resguardo para abrir (confirmado o anulado, y con jugador) y copiar los
/// datos de cada paso sin el documento del jugador.
/// No consulta la base de datos ni decide quién ve qué.
/// </summary>
public static class MapperPagos
{
    /// <summary>El cargo con lo pagado y lo pendiente.</summary>
    public static CargoDto ACargo(CargoConAplicado cargo) => new(
        cargo.CargoId,
        cargo.Tipo,
        cargo.Mes,
        cargo.Valor,
        Math.Min(cargo.Aplicado, cargo.Valor),
        ReglaEstadoDeCuenta.Pendiente(cargo.Valor, cargo.Aplicado));

    /// <summary>El pago, con si tiene soporte y si tiene resguardo.</summary>
    public static PagoDto APago(Pago pago, bool tieneSoporte) => new(
        pago.Id,
        pago.UsuarioRolId,
        pago.JugadorNombre,
        pago.Estado,
        pago.Valor,
        pago.ValorInformado,
        pago.Medio,
        pago.Entidad,
        pago.Referencia,
        pago.Fecha,
        pago.Concepto,
        pago.NumeroRecibo,
        pago.SaldoTrasPago,
        tieneSoporte,
        pago.UsuarioRolId is not null && ReglaTransicionPago.TieneRecibo(pago.Estado),
        pago.CreadoEn,
        pago.CreadoPorNombre,
        pago.ConfirmadoEn,
        pago.ConfirmadoPorNombre,
        pago.RechazadoEn,
        pago.RechazadoPorNombre,
        pago.MotivoRechazo,
        pago.AnuladoEn,
        pago.AnuladoPorNombre,
        pago.MotivoAnulacion);

    /// <summary>Saldo y estado de un jugador con esos cargos.</summary>
    public static ReglaEstadoDeCuenta.Cuenta ACuenta(IEnumerable<CargoConAplicado> cargos) =>
        ReglaEstadoDeCuenta.Calcular(cargos.Select(cargo => new ReglaEstadoDeCuenta.ValorDeCargo(cargo.Valor, cargo.Aplicado)));
}
