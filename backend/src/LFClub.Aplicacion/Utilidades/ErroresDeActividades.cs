namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato de los entrenamientos, los partidos, la convocatoria, el
/// resultado, la asistencia y las respuestas de las familias (spec 009).
/// Su responsabilidad es que todos los casos de uso respondan con el mismo código y con un texto
/// que le diga a la persona qué puede hacer.
/// No decide cuándo se produce cada error ni lleva el de una actividad que no se puede ver, que es
/// el <c>no_encontrado</c> común (RF-011).
/// </summary>
public static class ErroresDeActividades
{
    /// <summary>403: ve la actividad pero no entrena todas sus categorías ni es PRESIDENTE.</summary>
    public static ExcepcionDeAplicacion NoGestionaActividad() => new(
        "no_gestiona_actividad", 403,
        "Solo puede hacer esto el presidente o quien entrena todas las categorías de esta actividad.");

    /// <summary>409: la actividad está cancelada y no admite cambios, respuestas ni asistencia.</summary>
    public static ExcepcionDeAplicacion ActividadCancelada() => ExcepcionDeAplicacion.Conflicto(
        "actividad_cancelada", "Esta actividad está cancelada. Reactívala antes, si todavía no empieza.");

    /// <summary>409: solo se reactiva una actividad cancelada.</summary>
    public static ExcepcionDeAplicacion ActividadNoCancelada() => ExcepcionDeAplicacion.Conflicto(
        "actividad_no_cancelada", "Esta actividad no está cancelada. No se cambió nada.");

    /// <summary>409: la actividad ya empezó; no se reactiva ni se responde.</summary>
    public static ExcepcionDeAplicacion ActividadEmpezada() => ExcepcionDeAplicacion.Conflicto(
        "actividad_empezada", "Esta actividad ya empezó.");

    /// <summary>409: la asistencia se registra desde el día del entrenamiento.</summary>
    public static ExcepcionDeAplicacion EntrenamientoFuturo() => ExcepcionDeAplicacion.Conflicto(
        "entrenamiento_futuro", "La asistencia se registra desde el día del entrenamiento.");

    /// <summary>409: el resultado se registra cuando el partido ya empezó (RF-015).</summary>
    public static ExcepcionDeAplicacion PartidoSinEmpezar() => ExcepcionDeAplicacion.Conflicto(
        "partido_sin_empezar", "El resultado se registra cuando el partido ya empezó.");

    /// <summary>409: un partido jugado no se cancela ni cambia sus datos o su convocatoria (RF-021).</summary>
    public static ExcepcionDeAplicacion PartidoJugado() => ExcepcionDeAplicacion.Conflicto(
        "partido_jugado", "Este partido ya se jugó: solo se puede corregir su resultado.");

    /// <summary>409: el rival solo se corrige en un partido ya jugado; uno próximo se edita completo.</summary>
    public static ExcepcionDeAplicacion PartidoNoJugado() => ExcepcionDeAplicacion.Conflicto(
        "partido_no_jugado", "Este partido todavía no se juega: cambia su rival al editarlo.");

    /// <summary>409: una de las categorías no está activa o no es del club (RF-005).</summary>
    public static ExcepcionDeAplicacion CategoriaInactiva() => ExcepcionDeAplicacion.Conflicto(
        "categoria_inactiva", "Una de las categorías no está activa. Elige categorías activas del club.");

    /// <summary>409: las categorías de una actividad no cambian después de crearla (research §16.1).</summary>
    public static ExcepcionDeAplicacion CategoriasNoCambian() => ExcepcionDeAplicacion.Conflicto(
        "categorias_no_cambian",
        "Las categorías de una actividad no se cambian. Cancélala y crea otra con las categorías nuevas.");

    /// <summary>409: el jugador no es activo o no es de una categoría que se pueda usar aquí (RF-009, RF-022).</summary>
    public static ExcepcionDeAplicacion JugadorFueraDeLaActividad() => ExcepcionDeAplicacion.Conflicto(
        "jugador_fuera_de_la_actividad", "Uno de los jugadores no es de las categorías de esta actividad.");

    /// <summary>400: se enviaron goles a quien no jugó (RF-017).</summary>
    public static ExcepcionDeAplicacion GolesDeQuienNoJugo() => new(
        "goles_de_quien_no_jugo", 400, "Solo marca goles quien jugó.");

    /// <summary>400: los goles de los jugadores suman más que los del club (RF-017).</summary>
    public static ExcepcionDeAplicacion GolesSuperanMarcador() => new(
        "goles_superan_marcador", 400, "Los goles de los jugadores suman más que los goles del club.");

    /// <summary>400: la familia publica en un partido sin marcador y no lo envía entero (spec 010, RF-001).</summary>
    public static ExcepcionDeAplicacion FaltaMarcador() => new(
        "falta_marcador", 400, "Escribe el marcador del partido.");

    /// <summary>409: la familia envía un marcador cuando ya hay uno publicado, aunque sea igual (spec 010, RF-003).</summary>
    public static ExcepcionDeAplicacion MarcadorYaPublicado() => ExcepcionDeAplicacion.Conflicto(
        "marcador_ya_publicado", "El marcador ya fue publicado: solo puedes indicar lo de tu jugador.");

    /// <summary>409: quien entrena o el PRESIDENTE ya corrigió lo del jugador (spec 010, RF-012).</summary>
    public static ExcepcionDeAplicacion IndicacionCorregida() => ExcepcionDeAplicacion.Conflicto(
        "indicacion_corregida", "Quien entrena ya corrigió lo de tu jugador.");

    /// <summary>400: una fila del resultado es de alguien que no está convocado.</summary>
    public static ExcepcionDeAplicacion JugadorNoConvocado() => new(
        "jugador_no_convocado", 400, "Uno de los jugadores no está convocado a este partido.");
}
