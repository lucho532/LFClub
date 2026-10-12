namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato propios de la carta del jugador (spec 010).
/// Su responsabilidad es que todos los casos de uso de la carta respondan con el mismo código y con
/// un texto que le diga a la persona qué puede hacer.
/// No decide cuándo se produce cada error ni lleva el de una carta que no se puede ver, que es el
/// <c>no_encontrado</c> común, ni el <c>rol_no_autorizado</c> de los roles que nunca pueden hacerlo.
/// </summary>
public static class ErroresDeCarta
{
    /// <summary>
    /// 403: ve la carta pero no entrena la categoría activa del jugador ni es el PRESIDENTE (ni, para
    /// dorsal y posición, su familia) (RF-021, RF-027).
    /// </summary>
    public static ExcepcionDeAplicacion NoEntrenaCategoria() => new(
        "no_entrena_categoria", 403,
        "Solo quien entrena la categoría del jugador o el PRESIDENTE puede cambiar esto.");

    /// <summary>409: la carta de un jugador retirado ya no se cambia (research §12.2).</summary>
    public static ExcepcionDeAplicacion JugadorRetirado() => ExcepcionDeAplicacion.Conflicto(
        "jugador_retirado", "Este jugador está retirado del club: su carta ya no se cambia.");
}
