using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta la campana de la cuenta de un jugador (spec 012, RF-018 a
/// RF-021).
/// Su responsabilidad es pedir a cada repositorio, con el mismo cursor y el límite más uno, sus
/// comunicados y avisos, mezclarlos por identificador (en orden de bytes, como PostgreSQL), tomar el
/// límite (de 1 a 50, 30 por defecto), calcular si la actividad de cada aviso sigue disponible y
/// sumar los dos contadores, cada elemento una vez por cuenta (research §3).
/// No modifica datos y no marca la lectura.
/// </summary>
public class ServicioConsultaCampana : IServicioConsultaCampana
{
    private const int LimitePorDefecto = 30;
    private const int LimiteMaximo = 50;

    private readonly IRepositorioComunicados _comunicados;
    private readonly IRepositorioAvisos _avisos;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaCampana(IRepositorioComunicados comunicados, IRepositorioAvisos avisos)
    {
        _comunicados = comunicados;
        _avisos = avisos;
    }

    /// <inheritdoc />
    public async Task<PaginaDeCampanaDto> ListarAsync(
        int? limite,
        Guid? antesDe,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default)
    {
        ServicioConsultaComunicados.ExigirJugador(quienPregunta);
        var tamano = Math.Clamp(limite ?? LimitePorDefecto, 1, LimiteMaximo);
        var cuenta = quienPregunta.UsuarioId;

        var comunicados = await _comunicados.ListarRecibidosAsync(cuenta, limitacion, antesDe, tamano + 1, cancelacion);
        var avisos = await _avisos.ListarVisiblesAsync(cuenta, limitacion, antesDe, tamano + 1, cancelacion);

        var mezcla = comunicados
            .Select(fila => new ElementoMezclado(fila.Comunicado.Id, fila, null))
            .Concat(avisos.Select(fila => new ElementoMezclado(fila.Aviso.Id, null, fila)))
            .OrderBy(elemento => elemento.Id, Comparer<Guid>.Create(OrdenDeIdentificadores.CompararDescendente))
            .ToList();
        var pagina = mezcla.Take(tamano).ToList();

        var disponibles = await DisponiblesAsync(
            _avisos,
            pagina.Where(elemento => elemento.Aviso is not null).Select(elemento => elemento.Aviso!),
            quienPregunta,
            limitacion,
            cancelacion);
        var elementos = pagina
            .Select(elemento => elemento.Comunicado is { } comunicado
                ? MapperCampana.ADeComunicado(MapperComunicados.ARecibido(comunicado))
                : MapperCampana.ADeAviso(MapperCampana.AAviso(
                    elemento.Aviso!, disponibles.Contains(elemento.Aviso!.Aviso.ActividadId))))
            .ToList();

        var sinLeer = await ContarAsync(quienPregunta, limitacion, cancelacion);
        return new PaginaDeCampanaDto(elementos, mezcla.Count > tamano, sinLeer);
    }

    /// <inheritdoc />
    public async Task<ContadorDeCampanaDto> ContarSinLeerAsync(
        UsuarioRol quienPregunta, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default)
    {
        ServicioConsultaComunicados.ExigirJugador(quienPregunta);
        return new ContadorDeCampanaDto(await ContarAsync(quienPregunta, limitacion, cancelacion));
    }

    /// <summary>Comunicados más avisos sin leer de la cuenta.</summary>
    private async Task<int> ContarAsync(
        UsuarioRol quienPregunta, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion) =>
        await _comunicados.ContarSinLeerAsync(quienPregunta.UsuarioId, limitacion, cancelacion)
        + await _avisos.ContarSinLeerAsync(quienPregunta.UsuarioId, limitacion, cancelacion);

    /// <summary>De las actividades de esos avisos, las que la cuenta puede abrir hoy.</summary>
    internal static async Task<IReadOnlySet<Guid>> DisponiblesAsync(
        IRepositorioAvisos repositorio,
        IEnumerable<AvisoDeCuenta> avisos,
        UsuarioRol quienPregunta,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion)
    {
        var actividades = avisos.Select(fila => fila.Aviso).ToList();
        var entrenamientos = actividades
            .Where(aviso => aviso.ActividadTipo == TipoDeActividad.ENTRENAMIENTO)
            .Select(aviso => aviso.ActividadId)
            .Distinct()
            .ToList();
        var partidos = actividades
            .Where(aviso => aviso.ActividadTipo == TipoDeActividad.PARTIDO)
            .Select(aviso => aviso.ActividadId)
            .Distinct()
            .ToList();
        return await repositorio.DisponibilidadAsync(
            quienPregunta.UsuarioId, limitacion, entrenamientos, partidos, cancelacion);
    }

    /// <summary>Un comunicado o un aviso de la página, con su identificador para ordenar.</summary>
    private sealed record ElementoMezclado(Guid Id, RecibidoDeCuenta? Comunicado, AvisoDeCuenta? Aviso);
}
