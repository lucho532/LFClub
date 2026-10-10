namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato de las categorías, sus equipos, sus entrenadores y sus
/// jugadores.
/// Su responsabilidad es que todos los casos de uso respondan con el mismo código y con un texto
/// que le diga a la persona qué puede hacer.
/// No decide cuándo se produce cada error.
/// </summary>
public static class ErroresDeCategorias
{
    /// <summary>409: el club ya tiene activa la categoría de ese año (RF-003).</summary>
    public static ExcepcionDeAplicacion CategoriaYaExiste(int anio) => ExcepcionDeAplicacion.Conflicto(
        "categoria_ya_existe", $"Tu club ya tiene la categoría {anio}.");

    /// <summary>409: el club ya tiene la categoría de ese año, pero inactiva (RF-003).</summary>
    public static ExcepcionDeAplicacion CategoriaInactivaYaExiste(int anio) => ExcepcionDeAplicacion.Conflicto(
        "categoria_inactiva_ya_existe",
        $"Tu club ya tiene la categoría {anio}, pero está inactiva. Puedes reactivarla.");

    /// <summary>409: no se desactiva una categoría que tiene jugadores (RF-005).</summary>
    public static ExcepcionDeAplicacion CategoriaConJugadores() => ExcepcionDeAplicacion.Conflicto(
        "categoria_con_jugadores",
        "Esta categoría todavía tiene jugadores. Antes pásalos a otra categoría o retíralos del club.");

    /// <summary>409: no se borra una categoría que tiene o tuvo jugadores o entrenadores (RF-004a).</summary>
    public static ExcepcionDeAplicacion CategoriaConHistorial() => ExcepcionDeAplicacion.Conflicto(
        "categoria_con_historial",
        "Esta categoría tiene o tuvo jugadores o entrenadores, así que no se puede borrar. Solo puedes desactivarla.");

    /// <summary>409: una categoría inactiva no admite jugadores, entrenadores ni equipos (RF-007).</summary>
    public static ExcepcionDeAplicacion CategoriaInactiva() => ExcepcionDeAplicacion.Conflicto(
        "categoria_inactiva", "Esta categoría está inactiva. Reactívala para poder usarla.");

    /// <summary>409: solo se asigna a un ENTRENADOR, un DIRECTIVO o un PRESIDENTE aprobado (RF-017).</summary>
    public static ExcepcionDeAplicacion NoAsignableComoEntrenador() => ExcepcionDeAplicacion.Conflicto(
        "no_asignable_como_entrenador",
        "Solo puedes asignar a un entrenador, un directivo o un presidente de tu club con el ingreso aprobado.");

    /// <summary>409: solo dirige un equipo quien está asignado a su categoría (RF-028).</summary>
    public static ExcepcionDeAplicacion EntrenadorNoAsignado() => ExcepcionDeAplicacion.Conflicto(
        "entrenador_no_asignado",
        "Esa persona no está asignada a esta categoría. Asígnala antes de indicar qué equipos dirige.");

    /// <summary>409: la categoría ya tiene un equipo activo con ese nombre (RF-023).</summary>
    public static ExcepcionDeAplicacion EquipoYaExiste() => ExcepcionDeAplicacion.Conflicto(
        "equipo_ya_existe", "Esta categoría ya tiene un equipo con ese nombre. Elige otro.");

    /// <summary>409: no se borra un equipo que tiene o tuvo jugadores o entrenadores (RF-024a).</summary>
    public static ExcepcionDeAplicacion EquipoConHistorial() => ExcepcionDeAplicacion.Conflicto(
        "equipo_con_historial",
        "Este equipo tiene o tuvo jugadores o entrenadores, así que no se puede borrar. Solo puedes desactivarlo.");

    /// <summary>409: un jugador solo juega en equipos de su propia categoría (RF-025).</summary>
    public static ExcepcionDeAplicacion JugadorDeOtraCategoria() => ExcepcionDeAplicacion.Conflicto(
        "jugador_de_otra_categoria",
        "Ese jugador no está en la categoría de este equipo. Pásalo antes a esta categoría.");

    /// <summary>409: solo los jugadores aprobados y no retirados tienen categoría y se retiran (RF-012, RF-041).</summary>
    public static ExcepcionDeAplicacion NoEsJugador() => ExcepcionDeAplicacion.Conflicto(
        "no_es_jugador", "Esto solo se puede hacer con un jugador aprobado y activo del club.");

    /// <summary>
    /// 409: el documento del jugador está activo con otra cuenta en otro club, y mientras lo esté no
    /// se le reincorpora (RF-041 de la 006). No nombra ese club ni da datos de la otra cuenta.
    /// </summary>
    public static ExcepcionDeAplicacion DocumentoActivoEnOtroClub() => ExcepcionDeAplicacion.Conflicto(
        "documento_activo_en_otro_club",
        "Este documento está activo en otro club. Para reincorporarlo, primero debe retirarse de allí, "
        + "por su presidente o por su familia.");

    /// <summary>409: solo se reincorpora a un jugador que está retirado (RF-045).</summary>
    public static ExcepcionDeAplicacion JugadorNoRetirado() => ExcepcionDeAplicacion.Conflicto(
        "jugador_no_retirado", "Ese jugador no está retirado. No se cambió nada.");
}
