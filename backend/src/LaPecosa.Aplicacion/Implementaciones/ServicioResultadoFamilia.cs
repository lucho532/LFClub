using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que la familia de un convocado publica el resultado de un partido
/// o indica lo de su jugador (constitución §17.3; spec 010, RF-001 a RF-012; research §3 a §5).
/// Su responsabilidad es admitir solo la cuenta de un jugador, sobre el jugador de la petición y
/// solo si está convocado (si no, <c>404 no_encontrado</c>, también en un partido jugado); tomar el
/// bloqueo del club y volver a leer el partido, su fila y la suma de goles de los demás después de
/// bloquear, para que valga el primer marcador y la suma no pase del marcador aunque dos familias o
/// una familia y quien entrena escriban a la vez; validar con <see cref="ReglaResultadoDeFamilia"/>;
/// publicar el marcador si no lo había (a nombre del jugador, copiado) y guardar lo de su jugador
/// como indicado por la familia. Responde con el partido como lo ve la familia.
/// No escribe ni devuelve nunca lo de otro convocado, ni la suma de sus goles (RF-005, RF-006); no
/// corrige lo que ya corrigió quien entrena (RF-012) y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioResultadoFamilia : IServicioResultadoFamilia
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioPartidos _partidos;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly IRepositorioClub _club;
    private readonly IServicioConsultaPartidos _consulta;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioResultadoFamilia(
        AccesoAActividad acceso,
        IRepositorioPartidos partidos,
        IRepositorioConvocatoria convocatoria,
        IRepositorioClub club,
        IServicioConsultaPartidos consulta,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _partidos = partidos;
        _convocatoria = convocatoria;
        _club = club;
        _consulta = consulta;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PartidoDto> PublicarAsync(
        Guid partidoId, ResultadoDeFamiliaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        if (quienPregunta.Rol != Rol.JUGADOR)
        {
            throw new ExcepcionDeAplicacion("rol_no_autorizado", 403, "Tu rol en este club no permite hacer esto.");
        }

        // Quien no está convocado no sabe de este partido, aunque ya esté jugado (escenario 1.3).
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        if (visible.MiFila is null)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var partido = await _partidos.ObtenerAsync(partidoId, cancelacion);
                var fila = await _partidos.FilaDeAsync(partidoId, quienPregunta.Id, cancelacion);
                if (partido is null || fila is null)
                {
                    throw ExcepcionDeAplicacion.NoEncontrado();
                }

                var golesDeLosDemas = await _convocatoria.GolesDeLosDemasAsync(partidoId, quienPregunta.Id, cancelacion);
                var problema = ReglaResultadoDeFamilia.Validar(
                    partido.Estado,
                    visible.Empezo,
                    partido.GolesClub,
                    partido.GolesRival,
                    datos.GolesClub,
                    datos.GolesRival,
                    fila.IndicadoPor,
                    datos.Jugo,
                    datos.Goles,
                    golesDeLosDemas);
                if (problema is not null)
                {
                    throw Error(problema.Value);
                }

                var ahora = _reloj.AhoraUtc;
                var nombre = $"{quienPregunta.Nombres} {quienPregunta.Apellidos}";
                if (partido.GolesClub is null)
                {
                    await _convocatoria.PublicarPorFamiliaAsync(
                        partidoId, datos.GolesClub!.Value, datos.GolesRival!.Value, ahora, quienPregunta.UsuarioId, nombre, cancelacion);
                }

                await _convocatoria.IndicarPorFamiliaAsync(
                    partidoId, quienPregunta.Id, datos.Jugo, datos.Goles, ahora, quienPregunta.UsuarioId, nombre, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    private static ExcepcionDeAplicacion Error(ProblemaDeResultadoDeFamilia problema) => problema switch
    {
        ProblemaDeResultadoDeFamilia.PartidoCancelado => ErroresDeActividades.ActividadCancelada(),
        ProblemaDeResultadoDeFamilia.PartidoSinEmpezar => ErroresDeActividades.PartidoSinEmpezar(),
        ProblemaDeResultadoDeFamilia.FaltaMarcador => ErroresDeActividades.FaltaMarcador(),
        ProblemaDeResultadoDeFamilia.MarcadorYaPublicado => ErroresDeActividades.MarcadorYaPublicado(),
        ProblemaDeResultadoDeFamilia.IndicacionCorregida => ErroresDeActividades.IndicacionCorregida(),
        ProblemaDeResultadoDeFamilia.GolesDeQuienNoJugo => ErroresDeActividades.GolesDeQuienNoJugo(),
        ProblemaDeResultadoDeFamilia.GolesSuperanMarcador => ErroresDeActividades.GolesSuperanMarcador(),
        _ => new ExcepcionDeAplicacion(
            "datos_invalidos",
            400,
            "Revisa los datos marcados.",
            new Dictionary<string, string[]> { ["goles"] = ["Los goles van de 0 a 99."] }),
    };
}
