using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta la carta de un jugador (spec 010, RF-019 a RF-028).
/// Su responsabilidad es resolver el acceso con <see cref="AccesoACarta"/>, leer la carta guardada
/// (si la hay), la fecha de su foto y su asistencia a los entrenamientos de la temporada (el año de
/// hoy en hora de Colombia), y calcular la rareza con <see cref="ReglaRarezaDeCarta"/> y la valoración
/// con <see cref="ReglaValoracionDeCarta"/>. Las operaciones que cambian la carta responden con lo
/// que arma este servicio.
/// No modifica datos, no entrega cartas de más de un jugador (RF-025) y no accede al contexto de
/// Entity Framework.
/// </summary>
public class ServicioConsultaCarta : IServicioConsultaCarta
{
    private readonly AccesoACarta _acceso;
    private readonly IRepositorioCartas _cartas;
    private readonly IRepositorioFotosJugador _fotos;
    private readonly IRepositorioEstadisticas _estadisticas;
    private readonly ResolutorDeEnlaces _enlaces;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaCarta(
        AccesoACarta acceso,
        IRepositorioCartas cartas,
        IRepositorioFotosJugador fotos,
        IRepositorioEstadisticas estadisticas,
        ResolutorDeEnlaces enlaces,
        IReloj reloj)
    {
        _acceso = acceso;
        _cartas = cartas;
        _fotos = fotos;
        _estadisticas = estadisticas;
        _enlaces = enlaces;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<CartaDto> ObtenerAsync(Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);
        var carta = await _cartas.ObtenerAsync(usuarioRolId, cancelacion);
        var foto = await _fotos.FechaAsync(usuarioRolId, cancelacion);
        var rareza = await RarezaAsync(usuarioRolId, cancelacion);

        UltimoCambioDto? ultimoCambio = null;
        if (alcance.CambiaEstadisticas && carta?.EstadisticasCambiadasEn is { } cambiadasEn)
        {
            var autores = await _enlaces.AutoresAsync([carta.EstadisticasCambiadasPorUsuarioId], cancelacion);
            ultimoCambio = new UltimoCambioDto(
                cambiadasEn,
                carta.EstadisticasCambiadasPorNombre ?? string.Empty,
                false,
                carta.EstadisticasCambiadasPorUsuarioId is { } autorId ? autores.GetValueOrDefault(autorId) : null);
        }

        return MapperCarta.ACarta(jugador, carta, foto, rareza, alcance, ultimoCambio);
    }

    /// <inheritdoc />
    public async Task<RarezaCarta> RarezaAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var anio = HoraDeColombia.Ahora(_reloj).Hoy.Year;
        var (registrados, asistidos) = await _estadisticas.AsistenciaEntreAsync(
            usuarioRolId, new DateOnly(anio, 1, 1), new DateOnly(anio, 12, 31), cancelacion);
        return ReglaRarezaDeCarta.Calcular(registrados, CalculoDeAsistencia.Porcentaje(asistidos, registrados));
    }
}
