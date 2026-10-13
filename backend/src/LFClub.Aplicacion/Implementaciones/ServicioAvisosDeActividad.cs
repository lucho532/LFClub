using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que genera los avisos de actividades (spec 012, RF-001 a RF-017 y RF-025;
/// research §5, §7 y §10).
/// Su responsabilidad es, por cada hecho, redactar la línea con <see cref="RedaccionDeAvisos"/>,
/// decidir la cobertura con <see cref="ReglaDestinatariosDeAviso"/>, resolver los jugadores activos
/// en una consulta del club, excluir a la cuenta que hizo la acción (RF-005) y guardar el aviso con
/// una fila por jugador. No guarda nada si no hay destinatarios. Corre dentro de la transacción del
/// servicio que hizo el cambio, que decide si hubo un cambio real.
/// No abre transacciones, no llama a ningún servicio externo (RF-027) y no nombra a otros jugadores
/// en el aviso de asistencia (RF-016).
/// </summary>
public partial class ServicioAvisosDeActividad : IServicioAvisosDeActividad
{
    private readonly IRepositorioAvisos _avisos;
    private readonly IRepositorioClub _club;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioAvisosDeActividad(IRepositorioAvisos avisos, IRepositorioClub club, IReloj reloj)
    {
        _avisos = avisos;
        _club = club;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public Task EntrenamientoProgramadoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDeEntrenamientoAsync(
            entrenamiento,
            HechoDeActividad.ENTRENAMIENTO_PROGRAMADO,
            RedaccionDeAvisos.EntrenamientoProgramado(entrenamiento.Fecha, entrenamiento.HoraInicio, Hoy()),
            [],
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task EntrenamientoCambiadoAsync(
        Entrenamiento entrenamiento, CambiosDeActividad cambios, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDeEntrenamientoAsync(
            entrenamiento,
            HechoDeActividad.ENTRENAMIENTO_CAMBIADO,
            RedaccionDeAvisos.EntrenamientoCambiado(
                cambios, entrenamiento.Fecha, entrenamiento.HoraInicio, entrenamiento.Lugar, Hoy()),
            [],
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public async Task EntrenamientoAudienciaCambiadaAsync(
        Entrenamiento entrenamiento,
        IReadOnlyCollection<Guid> jugadoresQueEntran,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default)
    {
        if (jugadoresQueEntran.Count == 0)
        {
            return;
        }

        var destinatarios = await _avisos.ResolverDestinatariosAsync(
            CoberturaDeAviso.ALUDIDOS, [], null, jugadoresQueEntran, cuentaQueActua, cancelacion);
        if (destinatarios.Count == 0)
        {
            return;
        }

        var aviso = NuevoAviso(
            entrenamiento.ClubId,
            HechoDeActividad.ENTRENAMIENTO_PROGRAMADO,
            TipoDeActividad.ENTRENAMIENTO,
            entrenamiento.Id,
            RedaccionDeAvisos.EntrenamientoProgramado(entrenamiento.Fecha, entrenamiento.HoraInicio, Hoy()));
        await _avisos.GenerarAsync(aviso, destinatarios, cancelacion);
    }

    /// <inheritdoc />
    public Task EntrenamientoCanceladoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDeEntrenamientoAsync(
            entrenamiento,
            HechoDeActividad.ENTRENAMIENTO_CANCELADO,
            RedaccionDeAvisos.EntrenamientoCancelado(entrenamiento.Fecha, Hoy()),
            [],
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task EntrenamientoReactivadoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDeEntrenamientoAsync(
            entrenamiento,
            HechoDeActividad.ENTRENAMIENTO_REACTIVADO,
            RedaccionDeAvisos.EntrenamientoReactivado(entrenamiento.Fecha, Hoy()),
            [],
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task AsistenciaRegistradaAsync(
        Entrenamiento entrenamiento,
        IReadOnlyCollection<Guid> categoriasConCambio,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default) =>
        GenerarDeEntrenamientoAsync(
            entrenamiento,
            HechoDeActividad.ASISTENCIA_REGISTRADA,
            RedaccionDeAvisos.AsistenciaRegistrada(entrenamiento.Fecha, Hoy()),
            categoriasConCambio,
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task PartidoCambiadoAsync(
        Partido partido, CambiosDeActividad cambios, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDePartidoAsync(
            partido,
            HechoDeActividad.PARTIDO_CAMBIADO,
            RedaccionDeAvisos.PartidoCambiado(
                cambios, partido.Rival, partido.Fecha, partido.Hora, partido.Lugar, Hoy()),
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task PartidoCanceladoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDePartidoAsync(
            partido,
            HechoDeActividad.PARTIDO_CANCELADO,
            RedaccionDeAvisos.PartidoCancelado(partido.Rival, partido.Fecha, Hoy()),
            cuentaQueActua,
            cancelacion);

    /// <inheritdoc />
    public Task PartidoReactivadoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default) =>
        GenerarDePartidoAsync(
            partido,
            HechoDeActividad.PARTIDO_REACTIVADO,
            RedaccionDeAvisos.PartidoReactivado(partido.Rival, partido.Fecha, Hoy()),
            cuentaQueActua,
            cancelacion);

    private Task GenerarDeEntrenamientoAsync(
        Entrenamiento entrenamiento,
        HechoDeActividad hecho,
        string texto,
        IReadOnlyCollection<Guid> categorias,
        Guid cuentaQueActua,
        CancellationToken cancelacion) =>
        GenerarAsync(
            entrenamiento.ClubId,
            hecho,
            TipoDeActividad.ENTRENAMIENTO,
            entrenamiento.Id,
            texto,
            ReglaDestinatariosDeAviso.Evaluar(hecho),
            categorias,
            cuentaQueActua,
            cancelacion);

    private Task GenerarDePartidoAsync(
        Partido partido, HechoDeActividad hecho, string texto, Guid cuentaQueActua, CancellationToken cancelacion) =>
        GenerarAsync(
            partido.ClubId,
            hecho,
            TipoDeActividad.PARTIDO,
            partido.Id,
            texto,
            ReglaDestinatariosDeAviso.Evaluar(hecho, partido.Estado),
            partido.Categorias.Select(fila => fila.CategoriaId).ToList(),
            cuentaQueActua,
            cancelacion);

    private async Task GenerarAsync(
        Guid clubId,
        HechoDeActividad hecho,
        TipoDeActividad tipo,
        Guid actividadId,
        string texto,
        CoberturaDeAviso cobertura,
        IReadOnlyCollection<Guid> categorias,
        Guid cuentaQueActua,
        CancellationToken cancelacion)
    {
        var destinatarios = await _avisos.ResolverDestinatariosAsync(
            cobertura, categorias, actividadId, [], cuentaQueActua, cancelacion);
        if (destinatarios.Count == 0)
        {
            return;
        }

        await _avisos.GenerarAsync(NuevoAviso(clubId, hecho, tipo, actividadId, texto), destinatarios, cancelacion);
    }

    private AvisoDeActividad NuevoAviso(
        Guid clubId, HechoDeActividad hecho, TipoDeActividad tipo, Guid actividadId, string texto) =>
        new()
        {
            ClubId = clubId,
            Hecho = hecho,
            ActividadTipo = tipo,
            ActividadId = actividadId,
            Texto = texto,
            GeneradoEn = _reloj.AhoraUtc,
        };

    private DateOnly Hoy() => HoraDeColombia.Ahora(_reloj).Hoy;
}
