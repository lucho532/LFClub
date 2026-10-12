using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio del resumen del club.
/// Su responsabilidad es pedir las cuentas al repositorio y entregarlas en el DTO del contrato.
/// No decide nada: no hay regla de dominio, solo cuentas. No accede al contexto de Entity Framework
/// ni recibe un identificador de club.
/// </summary>
public class ServicioResumenClub : IServicioResumenClub
{
    private readonly IRepositorioResumenClub _resumen;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public ServicioResumenClub(IRepositorioResumenClub resumen)
    {
        _resumen = resumen;
    }

    /// <inheritdoc />
    public async Task<ResumenClubDto> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var (jugadores, equipos, entrenadores, categorias) = await _resumen.ContarAsync(cancelacion);
        return new ResumenClubDto(jugadores, equipos, entrenadores, categorias);
    }
}
