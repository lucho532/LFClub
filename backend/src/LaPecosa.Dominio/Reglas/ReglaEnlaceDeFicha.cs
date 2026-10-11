using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de a qué ficha lleva el nombre de una persona en una pantalla del club
/// (spec 008, RF-010 y RF-011; data-model, "Reglas de resolución").
/// Su responsabilidad es decidir, con el rol y el estado de ingreso del integrante que hoy existe en
/// el club, si el nombre lleva a la ficha del jugador, a la ficha básica o a ninguna. Recibe nulos
/// cuando ese integrante ya no existe (salió del club, fue rechazado o su cuenta se eliminó). Un
/// jugador retirado sigue siendo JUGADOR aprobado: su ficha se conserva.
/// No consulta datos ni concede acceso: abrir la ficha sigue pasando por sus reglas de acceso
/// (RF-012), y la pantalla solo pinta lo que esta regla decida.
/// </summary>
public static class ReglaEnlaceDeFicha
{
    /// <summary>
    /// Autor copiado de un retiro, una aprobación o un último cambio: ficha básica si su cuenta tiene
    /// en el club un integrante PRESIDENTE, DIRECTIVO o ENTRENADOR aprobado; si no, ninguna.
    /// </summary>
    public static TipoFicha? ParaAutor(Rol? rol, EstadoIngreso? estadoIngreso) =>
        TieneFichaBasica(rol, estadoIngreso) ? TipoFicha.BASICA : null;

    /// <summary>
    /// Persona de un ingreso aprobado, según su integrante de hoy: ficha del jugador si es JUGADOR
    /// aprobado (activo o retirado), ficha básica si es PRESIDENTE, DIRECTIVO o ENTRENADOR aprobado,
    /// y ninguna si ya no existe.
    /// </summary>
    public static TipoFicha? ParaPersonaAprobada(Rol? rol, EstadoIngreso? estadoIngreso) =>
        EsJugadorAprobado(rol, estadoIngreso) ? TipoFicha.JUGADOR
        : TieneFichaBasica(rol, estadoIngreso) ? TipoFicha.BASICA
        : null;

    /// <summary>
    /// Hermano desde cuya ficha se agregó a quien espera: ficha del jugador si es JUGADOR aprobado;
    /// en cualquier otro caso, ninguna.
    /// </summary>
    public static TipoFicha? ParaHermano(Rol? rol, EstadoIngreso? estadoIngreso) =>
        EsJugadorAprobado(rol, estadoIngreso) ? TipoFicha.JUGADOR : null;

    private static bool EsJugadorAprobado(Rol? rol, EstadoIngreso? estadoIngreso) =>
        rol == Rol.JUGADOR && estadoIngreso == EstadoIngreso.APROBADO;

    private static bool TieneFichaBasica(Rol? rol, EstadoIngreso? estadoIngreso) =>
        (rol is Rol.PRESIDENTE or Rol.DIRECTIVO or Rol.ENTRENADOR) && estadoIngreso == EstadoIngreso.APROBADO;
}
