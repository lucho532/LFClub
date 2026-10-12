using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la comprobación de acceso que comparten todas las operaciones sobre un entrenamiento
/// o un partido (spec 009, RF-011; research §4), como <see cref="AccesoAFicha"/> en la ficha.
/// Su responsabilidad es resolver quién pregunta (el integrante de la petición y las categorías que
/// entrena hoy, leídas en cada petición), leer la actividad, averiguar los hechos que piden las
/// reglas de visibilidad, aplicarlas y calcular lo que quien pregunta puede hacer con ella. Si la
/// actividad no existe, es de otro club o quien pregunta no puede verla, responde el mismo
/// <c>404 no_encontrado</c>, sin revelar cuál de los casos es.
/// No decide quién ve qué ni quién gestiona: eso es de las reglas del dominio. No arma la respuesta
/// ni cambia nada, y no comprueba el rol mínimo de cada operación, que ya hizo la autorización.
/// </summary>
public class AccesoAActividad
{
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly IRepositorioPartidos _partidos;
    private readonly IRepositorioAsignaciones _asignaciones;
    private readonly IReloj _reloj;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AccesoAActividad(
        IRepositorioEntrenamientos entrenamientos,
        IRepositorioPartidos partidos,
        IRepositorioAsignaciones asignaciones,
        IReloj reloj)
    {
        _entrenamientos = entrenamientos;
        _partidos = partidos;
        _asignaciones = asignaciones;
        _reloj = reloj;
    }

    /// <summary>
    /// Quien hace la petición: su rol, su categoría si es la cuenta de un jugador y las categorías
    /// que entrena hoy. La cuenta de un jugador no entrena ninguna: no se consulta.
    /// </summary>
    public async Task<QuienPregunta> QuienPreguntaAsync(UsuarioRol integrante, CancellationToken cancelacion = default)
    {
        if (integrante.Rol == Rol.JUGADOR)
        {
            return new QuienPregunta(integrante.Id, integrante.Rol, integrante.CategoriaId, new HashSet<Guid>());
        }

        var entrena = await _asignaciones.CategoriasDeAsync(integrante.Id, cancelacion);
        return new QuienPregunta(integrante.Id, integrante.Rol, null, entrena.ToHashSet());
    }

    /// <summary>
    /// El entrenamiento, lo que quien pregunta puede hacer con él y, para la cuenta de un jugador,
    /// su respuesta y su asistencia. Lanza <c>404 no_encontrado</c> si no existe en el club o quien
    /// pregunta no lo ve.
    /// </summary>
    public async Task<EntrenamientoVisible> EntrenamientoAsync(
        Guid entrenamientoId, UsuarioRol integrante, CancellationToken cancelacion = default)
    {
        var entrenamiento = await _entrenamientos.ObtenerAsync(entrenamientoId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var quien = await QuienPreguntaAsync(integrante, cancelacion);
        var categorias = entrenamiento.Categorias.Select(fila => fila.CategoriaId).ToList();

        var esDeSuCategoria = quien.CategoriaId is { } suCategoria && categorias.Contains(suCategoria);
        var miAsistencia = quien.EsJugador
            ? await _entrenamientos.AsistenciaDeAsync(entrenamientoId, quien.UsuarioRolId, cancelacion)
            : null;
        var laVe = ReglaVisibilidadDeEntrenamiento.LaVe(
            quien.Rol, categorias.Any(quien.CategoriasQueEntrena.Contains), esDeSuCategoria, miAsistencia is not null);
        if (!laVe)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
        var permisos = new PermisosDeActividadDto(
            ReglaGestionDeActividad.PuedeGestionar(quien.Rol, categorias, quien.CategoriasQueEntrena),
            ReglaAlcanceDeAsistencia.CategoriasMarcables(quien.Rol, categorias, quien.CategoriasQueEntrena).Count > 0,
            quien.EsJugador && esDeSuCategoria && entrenamiento.Estado == EstadoEntrenamiento.PROGRAMADO
                && !ReglaMomentoDeActividad.YaEmpezo(entrenamiento.Fecha, entrenamiento.HoraInicio, hoy, ahora));

        var miRespuesta = quien.EsJugador
            ? await _entrenamientos.RespuestaDeAsync(entrenamientoId, quien.UsuarioRolId, cancelacion)
            : null;
        return new EntrenamientoVisible(entrenamiento, quien, permisos, miRespuesta, miAsistencia);
    }

    /// <summary>
    /// El partido, lo que quien pregunta puede hacer con él y, para la cuenta de un jugador
    /// convocado, su fila. Lanza <c>404 no_encontrado</c> si no existe en el club o quien pregunta no
    /// lo ve: para una cuenta no convocada, un partido próximo no existe (§17.2).
    /// </summary>
    public async Task<PartidoVisible> PartidoAsync(Guid partidoId, UsuarioRol integrante, CancellationToken cancelacion = default)
    {
        var partido = await _partidos.ObtenerAsync(partidoId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var quien = await QuienPreguntaAsync(integrante, cancelacion);
        var categorias = partido.Categorias.Select(fila => fila.CategoriaId).ToList();
        var entrenaAlguna = categorias.Any(quien.CategoriasQueEntrena.Contains);
        var miFila = quien.EsJugador ? await _partidos.FilaDeAsync(partidoId, quien.UsuarioRolId, cancelacion) : null;
        if (!ReglaVisibilidadDePartido.LoVe(quien.Rol, partido.Estado, entrenaAlguna, miFila is not null))
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
        var empezo = ReglaMomentoDeActividad.YaEmpezo(partido.Fecha, partido.Hora, hoy, ahora);
        var permisos = new PermisosDeActividadDto(
            ReglaGestionDeActividad.PuedeGestionar(quien.Rol, categorias, quien.CategoriasQueEntrena),
            quien.Rol == Rol.PRESIDENTE || (!quien.EsJugador && entrenaAlguna),
            miFila is not null && partido.Estado == EstadoPartido.PROXIMO && !empezo,
            PartidoVisible.Participa(partido, miFila, empezo) && miFila!.IndicadoPor != OrigenResultado.GESTION);
        return new PartidoVisible(partido, quien, permisos, miFila, empezo);
    }

    /// <summary>Lanza <c>403 no_gestiona_actividad</c> si quien pregunta ve la actividad pero no la gestiona.</summary>
    public static void ExigirGestion(PermisosDeActividadDto permisos)
    {
        if (!permisos.Gestiona)
        {
            throw ErroresDeActividades.NoGestionaActividad();
        }
    }
}

/// <summary>
/// Representa un entrenamiento que quien pregunta puede ver, con lo que puede hacer con él.
/// Su responsabilidad es llevarlo junto a quien pregunta, sus permisos y, solo para la cuenta de un
/// jugador, su respuesta y su asistencia.
/// No lleva datos de otros jugadores.
/// </summary>
/// <param name="Entrenamiento">El entrenamiento con sus categorías.</param>
/// <param name="Quien">Quien pregunta.</param>
/// <param name="Permisos">Lo que puede hacer con él.</param>
/// <param name="MiRespuesta">La respuesta de su familia, si es la cuenta de un jugador y respondió.</param>
/// <param name="MiAsistencia">Su asistencia, si es la cuenta de un jugador y se registró.</param>
public record EntrenamientoVisible(
    Entrenamiento Entrenamiento,
    QuienPregunta Quien,
    PermisosDeActividadDto Permisos,
    RespuestaFamilia? MiRespuesta,
    bool? MiAsistencia);

/// <summary>
/// Representa un partido que quien pregunta puede ver, con lo que puede hacer con él.
/// Su responsabilidad es llevarlo junto a quien pregunta, sus permisos, si ya empezó y, solo para
/// la cuenta de un jugador convocado, su fila, y decir si esa cuenta participa del resultado.
/// No lleva los demás convocados.
/// </summary>
/// <param name="Partido">El partido con sus categorías.</param>
/// <param name="Quien">Quien pregunta.</param>
/// <param name="Permisos">Lo que puede hacer con él.</param>
/// <param name="MiFila">La fila del convocado de la petición, si es la cuenta de un jugador convocado.</param>
/// <param name="Empezo">Si ya empezó, en hora de Colombia.</param>
public record PartidoVisible(
    Partido Partido,
    QuienPregunta Quien,
    PermisosDeActividadDto Permisos,
    PartidoJugador? MiFila,
    bool Empezo)
{
    /// <summary>Un partido próximo que ya empezó y no tiene resultado.</summary>
    public bool FaltaResultado => Partido.Estado == EstadoPartido.PROXIMO && Empezo;

    /// <summary>
    /// La cuenta de un convocado a un partido que ya empezó (o ya está jugado) y no está cancelado:
    /// la que recibe lo de su propio jugador y puede publicar o indicar (spec 010).
    /// </summary>
    public static bool Participa(Partido partido, PartidoJugador? miFila, bool empezo) =>
        miFila is not null && partido.Estado != EstadoPartido.CANCELADO && (empezo || partido.Estado == EstadoPartido.JUGADO);
}
