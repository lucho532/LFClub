using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de las transiciones de estado de un partido y de lo que cada estado admite
/// (spec 009, RF-015, RF-021 y RF-038; research §7).
/// Su responsabilidad es decir qué se puede hacer desde cada estado: un partido próximo se cancela,
/// se cambia, se convoca y recibe su resultado; uno cancelado solo se reactiva; uno jugado solo
/// corrige su resultado, y nunca se cancela ni vuelve a próximo.
/// No comprueba quién lo pide ni si el partido ya empezó (ver <see cref="ReglaMomentoDeActividad"/>),
/// y no cambia el estado.
/// </summary>
public static class ReglaTransicionPartido
{
    /// <summary>Próximo → cancelado.</summary>
    public static bool PuedeCancelar(EstadoPartido estado) => estado == EstadoPartido.PROXIMO;

    /// <summary>Cancelado → próximo.</summary>
    public static bool PuedeReactivar(EstadoPartido estado) => estado == EstadoPartido.CANCELADO;

    /// <summary>Próximo → jugado, o jugado → jugado al corregirlo.</summary>
    public static bool PuedeRegistrarResultado(EstadoPartido estado) =>
        estado is EstadoPartido.PROXIMO or EstadoPartido.JUGADO;

    /// <summary>Rival, fecha, hora, lugar, condición, tipo y torneo solo cambian en un partido próximo.</summary>
    public static bool PuedeCambiarDatos(EstadoPartido estado) => estado == EstadoPartido.PROXIMO;

    /// <summary>La convocatoria solo cambia en un partido próximo.</summary>
    public static bool PuedeConvocar(EstadoPartido estado) => estado == EstadoPartido.PROXIMO;
}
