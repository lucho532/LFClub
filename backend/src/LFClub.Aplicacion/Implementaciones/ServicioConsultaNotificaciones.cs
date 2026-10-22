using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que lista las notificaciones de la pasarela para el DESARROLLADOR (spec 017,
/// RF-015).
/// Su responsabilidad es pedir al repositorio las últimas 100, con el filtro opcional por resultado, y
/// convertirlas en DTO.
/// No modifica nada y no expone el cuerpo ni la firma.
/// </summary>
public class ServicioConsultaNotificaciones : IServicioConsultaNotificaciones
{
    private const int Limite = 100;

    private readonly IRepositorioCobrosPlataforma _cobros;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public ServicioConsultaNotificaciones(IRepositorioCobrosPlataforma cobros)
    {
        _cobros = cobros;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificacionPasarelaDto>> ListarAsync(
        ResultadoNotificacion? resultado, CancellationToken cancelacion = default) =>
        (await _cobros.ListarNotificacionesAsync(resultado, Limite, cancelacion))
        .Select(MapperCobroPlataforma.ANotificacion)
        .ToList();
}
