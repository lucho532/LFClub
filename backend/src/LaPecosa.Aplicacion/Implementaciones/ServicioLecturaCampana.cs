using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que marca como leído lo que hay en la campana (spec 012, RF-019 a RF-022).
/// Su responsabilidad es poner la hora de lectura en todas las filas de la cuenta para un aviso,
/// solo donde todavía es nula, de modo que la primera lectura no se mueva y que leerlo desde un
/// hermano lo deje leído para toda la cuenta, y marcar todos los comunicados y avisos visibles de la
/// cuenta. Una sesión limitada a un jugador solo ve lo de ese jugador, pero marca la lectura de la
/// cuenta entera.
/// No marca nada de lo que no le llegó a la cuenta y nunca devuelve datos de la actividad más allá
/// de su tipo, su identificador y si sigue disponible (RF-017).
/// </summary>
public class ServicioLecturaCampana : IServicioLecturaCampana
{
    private readonly IRepositorioComunicados _comunicados;
    private readonly IRepositorioAvisos _avisos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioLecturaCampana(IRepositorioComunicados comunicados, IRepositorioAvisos avisos, IReloj reloj)
    {
        _comunicados = comunicados;
        _avisos = avisos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<AvisoRecibidoDto> MarcarAvisoLeidoAsync(
        Guid avisoId,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default)
    {
        ServicioConsultaComunicados.ExigirJugador(quienPregunta);
        var cuenta = quienPregunta.UsuarioId;
        var recibido = await _avisos.ObtenerParaLecturaAsync(cuenta, limitacion, avisoId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        if (!recibido.Leido)
        {
            await _avisos.MarcarLeidoAsync(cuenta, avisoId, _reloj.AhoraUtc, cancelacion);
            recibido = await _avisos.ObtenerParaLecturaAsync(cuenta, limitacion, avisoId, cancelacion)
                ?? throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var disponibles = await ServicioConsultaCampana.DisponiblesAsync(
            _avisos, [recibido], quienPregunta, limitacion, cancelacion);
        return MapperCampana.AAviso(recibido, disponibles.Contains(recibido.Aviso.ActividadId));
    }

    /// <inheritdoc />
    public async Task<ContadorDeCampanaDto> MarcarTodosAsync(
        UsuarioRol quienPregunta, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default)
    {
        ServicioConsultaComunicados.ExigirJugador(quienPregunta);
        var cuenta = quienPregunta.UsuarioId;
        await _comunicados.MarcarTodosLeidosAsync(cuenta, limitacion, _reloj.AhoraUtc, cancelacion);
        await _avisos.MarcarTodosLeidosAsync(cuenta, limitacion, _reloj.AhoraUtc, cancelacion);
        return new ContadorDeCampanaDto(
            await _comunicados.ContarSinLeerAsync(cuenta, limitacion, cancelacion)
            + await _avisos.ContarSinLeerAsync(cuenta, limitacion, cancelacion));
    }
}
