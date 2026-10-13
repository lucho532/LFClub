using LFClub.Dominio.Entidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de avisar a las familias de lo que pasa con un entrenamiento o un
/// partido (spec 012, historias 1, 2 y 4; research §5).
/// Su responsabilidad es, por cada hecho, redactar la línea, decidir a quién llega con la regla de
/// destinatarios, excluir a la cuenta que hizo la acción y guardarlo con una fila por jugador. Cada
/// operación se llama dentro de la transacción del servicio que hizo el cambio y solo cuando hubo un
/// cambio real: decidirlo es de quien llama.
/// No abre transacciones propias, no envía nada fuera de la aplicación y no guarda nada si no hay
/// destinatarios.
/// </summary>
public interface IServicioAvisosDeActividad
{
    /// <summary>Se programó un entrenamiento: avisa a los jugadores de sus categorías.</summary>
    Task EntrenamientoProgramadoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>Cambió la fecha, la hora o el lugar de un entrenamiento (con sus valores nuevos).</summary>
    Task EntrenamientoCambiadoAsync(
        Entrenamiento entrenamiento, CambiosDeActividad cambios, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>
    /// Avisa de "nuevo entrenamiento" solo a los jugadores que pasan a verlo al cambiar su audiencia
    /// (spec 013, RF-022). A quienes dejan de verlo no se les avisa. Sin jugadores que entren, no
    /// hace nada.
    /// </summary>
    Task EntrenamientoAudienciaCambiadaAsync(
        Entrenamiento entrenamiento,
        IReadOnlyCollection<Guid> jugadoresQueEntran,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default);

    /// <summary>Se canceló un entrenamiento.</summary>
    Task EntrenamientoCanceladoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>Se reactivó un entrenamiento cancelado.</summary>
    Task EntrenamientoReactivadoAsync(Entrenamiento entrenamiento, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>
    /// Se registró la asistencia de un entrenamiento. Solo avisa a las categorías indicadas, las
    /// únicas en las que una asistencia fue nueva o cambió; el texto no dice quién asistió.
    /// </summary>
    Task AsistenciaRegistradaAsync(
        Entrenamiento entrenamiento,
        IReadOnlyCollection<Guid> categoriasConCambio,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default);

    /// <summary>Cambió la fecha, la hora o el lugar de un partido: a sus convocados, o a sus categorías si está jugado.</summary>
    Task PartidoCambiadoAsync(
        Partido partido, CambiosDeActividad cambios, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>Se canceló un partido.</summary>
    Task PartidoCanceladoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>Se reactivó un partido cancelado.</summary>
    Task PartidoReactivadoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default);

    /// <summary>
    /// Se convocó a esos jugadores a un partido: un aviso por cuenta, con los nombres de sus
    /// jugadores convocados y filas de destinatario solo de ellos. Los que no son jugadores activos
    /// se ignoran.
    /// </summary>
    Task JugadorConvocadoAsync(
        Partido partido,
        IReadOnlyCollection<Guid> jugadoresConvocadosIds,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default);

    /// <summary>Se registró o corrigió el marcador: avisa a los jugadores de todas las categorías del partido.</summary>
    Task ResultadoPublicadoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default);
}
