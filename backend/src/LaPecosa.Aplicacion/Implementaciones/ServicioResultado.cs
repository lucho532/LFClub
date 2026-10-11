using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que registra y corrige el resultado de un partido (spec 009, RF-015 a
/// RF-020).
/// Su responsabilidad es exigir que quien pregunta vea y gestione el partido, que no esté cancelado
/// y que ya haya empezado (en hora de Colombia) o ya esté jugado; validar el resultado con
/// <see cref="ReglaResultadoDePartido"/>; y dejar el partido jugado, con el marcador, quiénes jugaron,
/// los goles y quién lo cambió, con su nombre copiado (§13), todo en una transacción.
/// No decide quién ve el partido, no guarda historial y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioResultado : IServicioResultado
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly IServicioConsultaPartidos _consulta;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioResultado(
        AccesoAActividad acceso,
        IRepositorioConvocatoria convocatoria,
        IServicioConsultaPartidos consulta,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _convocatoria = convocatoria;
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
        var partido = visible.Partido;
        if (!ReglaTransicionPartido.PuedeRegistrarResultado(partido.Estado))
        {
            throw ErroresDeActividades.ActividadCancelada();
        }

        if (partido.Estado == EstadoPartido.PROXIMO && !visible.Empezo)
        {
            throw ErroresDeActividades.PartidoSinEmpezar();
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var convocados = (await _convocatoria.ConvocadosAsync(partidoId, cancelacion))
                    .Select(fila => fila.UsuarioRolId)
                    .ToHashSet();
                var problema = ReglaResultadoDePartido.Validar(golesClub, golesRival, convocados, filas);
                if (problema is not null)
                {
                    throw Error(problema.Value);
                }

                await _convocatoria.GuardarResultadoAsync(
                    partidoId,
                    golesClub,
                    golesRival,
                    filas,
                    _reloj.AhoraUtc,
                    quienPregunta.UsuarioId,
                    $"{quienPregunta.Nombres} {quienPregunta.Apellidos}",
                    cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
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
