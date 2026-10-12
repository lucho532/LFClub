using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio con el que quien entrena las categorías del partido o el PRESIDENTE
/// registran y corrigen su resultado (spec 009, RF-015 a RF-020; spec 010, RF-007 a RF-012).
/// Su responsabilidad es exigir que quien pregunta vea y gestione el partido, que no esté cancelado
/// y que ya haya empezado (en hora de Colombia) o ya esté jugado; tomar el bloqueo del club y volver
/// a leer el partido y sus convocados, para que una familia que publica a la vez no deje la suma de
/// goles incoherente (research §3); validar el resultado con <see cref="ReglaResultadoDePartido"/>;
/// decidir fila por fila con <see cref="ReglaCorreccionDeIndicacion"/> qué convocados se corrigen; y
/// dejar el partido jugado con origen GESTION, el marcador, quiénes jugaron, los goles y quién lo
/// cambió, con su nombre copiado (§13), todo en una transacción. Si el partido no tenía marcador,
/// queda también como la primera publicación.
/// No atiende a la familia (eso es <see cref="ServicioResultadoFamilia"/>), no decide quién ve el
/// partido, no guarda historial y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioResultado : IServicioResultado
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly IRepositorioPartidos _partidos;
    private readonly IRepositorioClub _club;
    private readonly IServicioConsultaPartidos _consulta;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioResultado(
        AccesoAActividad acceso,
        IRepositorioConvocatoria convocatoria,
        IRepositorioPartidos partidos,
        IRepositorioClub club,
        IServicioConsultaPartidos consulta,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _convocatoria = convocatoria;
        _partidos = partidos;
        _club = club;
        _consulta = consulta;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PartidoDto> RegistrarAsync(
        Guid partidoId, RegistrarResultadoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var (golesClub, golesRival, filas) = Leer(datos);
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        AccesoAActividad.ExigirGestion(visible.Permisos);
        ExigirMomento(visible.Partido, visible.Empezo);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var partido = await _partidos.ObtenerAsync(partidoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
                ExigirMomento(partido, visible.Empezo);

                var convocados = await _convocatoria.ConvocadosAsync(partidoId, cancelacion);
                var problema = ReglaResultadoDePartido.Validar(
                    golesClub, golesRival, convocados.Select(fila => fila.UsuarioRolId).ToHashSet(), filas);
                if (problema is not null)
                {
                    throw Error(problema.Value);
                }

                await _convocatoria.GuardarResultadoAsync(
                    partidoId,
                    golesClub,
                    golesRival,
                    Guardadas(convocados, filas),
                    _reloj.AhoraUtc,
                    quienPregunta.UsuarioId,
                    $"{quienPregunta.Nombres} {quienPregunta.Apellidos}",
                    cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    private static void ExigirMomento(Partido partido, bool empezo)
    {
        if (!ReglaTransicionPartido.PuedeRegistrarResultado(partido.Estado))
        {
            throw ErroresDeActividades.ActividadCancelada();
        }

        if (partido.Estado == EstadoPartido.PROXIMO && !empezo)
        {
            throw ErroresDeActividades.PartidoSinEmpezar();
        }
    }

    // Los convocados que no se envían quedan como que no jugaron (RF-027 de la 009).
    private static List<FilaGuardada> Guardadas(IReadOnlyList<PartidoJugador> convocados, IReadOnlyList<FilaDeResultado> filas)
    {
        var enviadas = filas.ToDictionary(fila => fila.UsuarioRolId);
        return convocados
            .Select(fila =>
            {
                var jugo = enviadas.TryGetValue(fila.UsuarioRolId, out var enviada) && enviada.Jugo;
                var goles = jugo ? enviada.Goles : 0;
                var origen = ReglaCorreccionDeIndicacion.OrigenNuevo(fila.IndicadoPor, fila.Jugo, fila.Goles, jugo, goles);
                var corregida = origen == OrigenResultado.GESTION
                    && (fila.IndicadoPor != OrigenResultado.GESTION || fila.Jugo != jugo || fila.Goles != goles);
                return new FilaGuardada(fila.UsuarioRolId, jugo, goles, corregida);
            })
            .ToList();
    }

    private static (int GolesClub, int GolesRival, IReadOnlyList<FilaDeResultado> Filas) Leer(RegistrarResultadoDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.GolesClub is null)
        {
            errores.Agregar("golesClub", "Escribe los goles del club.");
        }

        if (datos.GolesRival is null)
        {
            errores.Agregar("golesRival", "Escribe los goles del rival.");
        }

        errores.LanzarSiHayErrores();
        var filas = (datos.Jugadores ?? [])
            .Select(fila => new FilaDeResultado(fila.UsuarioRolId, fila.Jugo, fila.Goles))
            .ToList();
        return (datos.GolesClub!.Value, datos.GolesRival!.Value, filas);
    }

    private static ExcepcionDeAplicacion Error(ProblemaDeResultado problema) => problema switch
    {
        ProblemaDeResultado.JugadorNoConvocado => ErroresDeActividades.JugadorNoConvocado(),
        ProblemaDeResultado.GolesDeQuienNoJugo => ErroresDeActividades.GolesDeQuienNoJugo(),
        ProblemaDeResultado.GolesSuperanMarcador => ErroresDeActividades.GolesSuperanMarcador(),
        _ => new ExcepcionDeAplicacion(
            "datos_invalidos",
            400,
            "Revisa los datos marcados.",
            new Dictionary<string, string[]>
            {
                ["golesClub"] = ["Los goles van de 0 a 99, y cada jugador aparece una sola vez."],
            }),
    };
}
