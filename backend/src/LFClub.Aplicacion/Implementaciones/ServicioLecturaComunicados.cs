using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que marca un comunicado como leído (spec 011, RF-020 a RF-023).
/// Su responsabilidad es poner la hora de lectura en todas las filas de la cuenta para ese
/// comunicado, solo donde todavía es nula, de modo que la primera lectura no se mueva y que leerlo
/// desde un hermano lo deje leído para toda la cuenta. Una sesión limitada a un jugador solo ve lo
/// de ese jugador, pero marca la lectura de la cuenta entera. "Marcar todos" pasó a la campana,
/// <see cref="ServicioLecturaCampana"/> (spec 012).
/// No marca nada de lo que no le llegó a la cuenta, no lo hace con comunicados retirados y no guarda
/// qué jugador lo abrió.
/// </summary>
public class ServicioLecturaComunicados : IServicioLecturaComunicados
{
    private readonly IRepositorioComunicados _comunicados;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioLecturaComunicados(IRepositorioComunicados comunicados, IReloj reloj)
    {
        _comunicados = comunicados;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<ComunicadoRecibidoDto> MarcarLeidoAsync(
        Guid comunicadoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default)
    {
        ServicioConsultaComunicados.ExigirJugador(quienPregunta);
        var recibido = await _comunicados.ObtenerRecibidoAsync(
            quienPregunta.UsuarioId, limitacion, comunicadoId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        if (!recibido.Leido)
        {
            await _comunicados.MarcarLeidoAsync(quienPregunta.UsuarioId, comunicadoId, _reloj.AhoraUtc, cancelacion);
            recibido = await _comunicados.ObtenerRecibidoAsync(
                quienPregunta.UsuarioId, limitacion, comunicadoId, cancelacion)
                ?? throw ExcepcionDeAplicacion.NoEncontrado();
        }

        return MapperComunicados.ARecibido(recibido);
    }
}
