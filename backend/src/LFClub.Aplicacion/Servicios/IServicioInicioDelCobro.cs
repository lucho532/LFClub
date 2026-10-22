using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el cambio del mes desde el que se cobra a un club (spec 017, RF-005a).
/// Su responsabilidad es fijar o borrar <c>MesInicioCobro</c> y devolver el estado de pago del club.
/// No genera cobros retroactivos: el trabajo solo crea el del mes en curso.
/// </summary>
public interface IServicioInicioDelCobro
{
    /// <summary>
    /// Fija el mes de inicio (<c>AAAA-MM</c>) o lo borra con nulo. Un formato inválido es un 400 y un
    /// club inexistente, un 404.
    /// </summary>
    Task<EstadoDePagoClubDto> FijarAsync(
        Guid clubId, FijarInicioDelCobroDto datos, CancellationToken cancelacion = default);
}
