using System.Globalization;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de los cobros de la plataforma en DTO (spec 017).
/// Su responsabilidad es armar el detalle por rol con lo guardado al generar el cobro, sin
/// recalcularlo, y dar el periodo como <c>AAAA-MM</c>.
/// No incluye llaves, secretos ni datos personales.
/// </summary>
public static class MapperCobroPlataforma
{
    /// <summary>Mes en formato <c>AAAA-MM</c>.</summary>
    public static string APeriodo(DateOnly mes) => mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    /// <summary>Cobro con su pago, si lo tiene.</summary>
    public static CobroPlataformaDto ACobro(
        CobroPlataforma cobro, PagoPlataforma? pago, bool enVerificacion = false, bool noAprobado = false) => new(
        cobro.Id,
        APeriodo(cobro.Periodo),
        cobro.Estado,
        [
            Linea(Rol.JUGADOR, cobro.Jugadores, cobro.PrecioJugador),
            Linea(Rol.ENTRENADOR, cobro.Entrenadores, cobro.PrecioEntrenador),
            Linea(Rol.DIRECTIVO, cobro.Directivos, cobro.PrecioDirectivo),
            Linea(Rol.PRESIDENTE, cobro.Presidentes, cobro.PrecioPresidente),
        ],
        cobro.Total,
        cobro.GeneradoEn,
        cobro.SuspendeDesde,
        pago?.PagadoEn,
        pago?.Origen,
        pago?.Referencia,
        pago?.MedioDePago,
        enVerificacion && !noAprobado,
        noAprobado,
        cobro.MotivoAnulacion);

    /// <summary>Estado de pago de un club a partir de sus cobros pendientes y el día de hoy en Colombia.</summary>
    public static EstadoDePagoClubDto AEstadoDePago(
        Club club, IReadOnlyCollection<CobroPlataforma> pendientes, DateOnly hoy) => new(
        ReglaEstadoDePagoClub.Calcular(
            club.Estado,
            club.MotivoSuspension,
            club.MesInicioCobro,
            pendientes.Select(cobro => new CobroParaSuspension(cobro.Estado, cobro.SuspendeDesde, cobro.SuspensionOmitida)),
            hoy),
        pendientes.Where(cobro => cobro.Estado == EstadoCobroPlataforma.PENDIENTE).Sum(cobro => cobro.Total),
        club.MesInicioCobro is { } mes ? APeriodo(mes) : null,
        club.Estado == EstadoClub.SUSPENDIDO ? club.MotivoSuspension : null);

    /// <summary>Notificación con el club y el periodo de su cobro.</summary>
    public static NotificacionPasarelaDto ANotificacion(NotificacionConCobro fila) => new(
        fila.Notificacion.Id,
        fila.Notificacion.RecibidaEn,
        fila.Notificacion.Resultado,
        fila.Notificacion.TransaccionId,
        fila.Notificacion.EstadoPasarela,
        fila.Notificacion.Referencia,
        fila.Notificacion.Valor,
        fila.ClubId,
        fila.Periodo is { } periodo ? APeriodo(periodo) : null);

    private static DetalleDeRolDto Linea(Rol rol, int integrantes, decimal precio) =>
        new(rol, integrantes, precio, integrantes * precio);
}
