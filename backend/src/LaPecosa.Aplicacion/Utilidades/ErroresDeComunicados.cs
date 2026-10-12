namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato propios de los comunicados del club (spec 011).
/// Su responsabilidad es que todos los casos de uso de los comunicados respondan con el mismo código
/// y con un texto que le diga a la persona qué puede hacer.
/// No decide cuándo se produce cada error ni lleva el de un comunicado que no se puede ver, que es
/// el <c>no_encontrado</c> común. El código <c>no_entrena_categoria</c> es el de la carta, con un
/// texto propio de los comunicados; <c>datos_invalidos</c> es el de la validación.
/// </summary>
public static class ErroresDeComunicados
{
    /// <summary>403: el rol de quien pregunta nunca puede hacerlo (también lo comprueba la autorización).</summary>
    public static ExcepcionDeAplicacion RolNoAutorizado() => new(
        "rol_no_autorizado", 403, "Tu rol en este club no permite hacer esto.");

    /// <summary>
    /// 403: el ENTRENADOR pidió una categoría o equipo de una categoría que no entrena, o todo el
    /// club (RF-005). Es el código <c>no_entrena_categoria</c> de la carta (spec 010).
    /// </summary>
    public static ExcepcionDeAplicacion NoEntrenaCategoria() => new(
        "no_entrena_categoria", 403,
        "Solo puedes dirigir comunicados a las categorías que entrenas y a sus equipos.");

    /// <summary>400: la audiencia elegida no es coherente o no existe en el club (research §4).</summary>
    public static ExcepcionDeAplicacion AudienciaInvalida() => new(
        "audiencia_invalida", 400, "La audiencia elegida no es válida.");

    /// <summary>400: la audiencia no tiene ningún jugador activo (RF-008).</summary>
    public static ExcepcionDeAplicacion SinDestinatarios() => new(
        "sin_destinatarios", 400,
        "Esa audiencia no tiene jugadores activos: el comunicado no le llegaría a nadie.");

    /// <summary>409: la clave de publicación ya la usó otro integrante (research §6).</summary>
    public static ExcepcionDeAplicacion ClaveEnUso() => ExcepcionDeAplicacion.Conflicto(
        "clave_en_uso", "Esa clave de publicación ya la usó otra persona.");

    /// <summary>409: el comunicado ya está retirado y no vuelve a publicarse (RF-027).</summary>
    public static ExcepcionDeAplicacion ComunicadoRetirado() => ExcepcionDeAplicacion.Conflicto(
        "comunicado_retirado", "El comunicado ya está retirado.");

    /// <summary>403: lo ve pero no lo publicó ni es el PRESIDENTE (research §7).</summary>
    public static ExcepcionDeAplicacion NoRetiraComunicado() => new(
        "no_retira_comunicado", 403,
        "Solo quien lo publicó o el PRESIDENTE puede retirar un comunicado.");
}
