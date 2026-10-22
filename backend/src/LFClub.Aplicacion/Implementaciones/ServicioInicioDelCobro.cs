using System.Globalization;
using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que fija desde qué mes se cobra a un club (spec 017, RF-005a).
/// Su responsabilidad es validar el mes <c>AAAA-MM</c>, guardarlo como primer día del mes (o borrarlo)
/// y devolver el estado de pago del club.
/// No crea cobros: los crea <see cref="IServicioCobrosPlataforma"/> en su próximo ciclo.
/// </summary>
public class ServicioInicioDelCobro : IServicioInicioDelCobro
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioCobrosPlataforma _cobros;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioInicioDelCobro(
        IRepositorioClubesPlataforma clubes,
        IRepositorioCobrosPlataforma cobros,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _clubes = clubes;
        _cobros = cobros;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<EstadoDePagoClubDto> FijarAsync(
        Guid clubId, FijarInicioDelCobroDto datos, CancellationToken cancelacion = default)
    {
        var mes = LeerMes(datos.MesDeInicio);
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();

        club.MesInicioCobro = mes;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);

        var pendientes = await _cobros.ListarPendientesAsync(clubId, cancelacion);
        return MapperCobroPlataforma.AEstadoDePago(club, pendientes, ReglaMesDeCobro.DiaEnColombia(_reloj.AhoraUtc));
    }

    private static DateOnly? LeerMes(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(
                texto.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mes))
        {
            var errores = new ErroresDeValidacion();
            errores.Agregar("mesDeInicio", "Indica el mes con el formato AAAA-MM, por ejemplo 2026-12.");
            errores.LanzarSiHayErrores();
        }

        return mes;
    }
}
