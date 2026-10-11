using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de un resultado válido (spec 009, RF-017, escenario 3.4).
/// Su responsabilidad es revisar el marcador y las filas de los convocados y devolver el primer
/// problema: goles fuera de 0 a 99, una fila de alguien que no está convocado, goles a quien no
/// jugó, o más goles de jugadores que goles del club. Que los goles de los jugadores sumen menos que
/// el marcador se acepta: puede haber goles en contra o sin autor conocido.
/// No consulta la base de datos, no comprueba el estado del partido ni si ya empezó, y no guarda
/// nada.
/// </summary>
public static class ReglaResultadoDePartido
{
    /// <summary>El primer problema del resultado, o nulo si es válido.</summary>
    public static ProblemaDeResultado? Validar(
        int golesClub, int golesRival, IReadOnlySet<Guid> convocados, IReadOnlyCollection<FilaDeResultado> filas)
    {
        if (!EnRango(golesClub) || !EnRango(golesRival) || filas.Any(fila => !EnRango(fila.Goles))
            || filas.DistinctBy(fila => fila.UsuarioRolId).Count() != filas.Count)
        {
            return ProblemaDeResultado.DatosFueraDeRango;
        }

        if (filas.Any(fila => !convocados.Contains(fila.UsuarioRolId)))
        {
            return ProblemaDeResultado.JugadorNoConvocado;
        }

        if (filas.Any(fila => !fila.Jugo && fila.Goles > 0))
        {
            return ProblemaDeResultado.GolesDeQuienNoJugo;
        }

        return filas.Sum(fila => fila.Goles) > golesClub ? ProblemaDeResultado.GolesSuperanMarcador : null;
    }

    private static bool EnRango(int goles) => goles is >= 0 and <= Partido.MaximoDeGoles;
}

/// <summary>
/// Representa lo que se envía de un convocado al registrar un resultado.
/// Su responsabilidad es llevar quién es, si jugó y cuántos goles marcó.
/// No lleva su categoría, que es la copiada al convocarlo.
/// </summary>
/// <param name="UsuarioRolId">Jugador convocado.</param>
/// <param name="Jugo">Si jugó.</param>
/// <param name="Goles">Goles que marcó.</param>
public readonly record struct FilaDeResultado(Guid UsuarioRolId, bool Jugo, int Goles);

/// <summary>
/// Representa por qué un resultado no es válido (RF-017).
/// Su responsabilidad es nombrar cada problema para que la aplicación lo traduzca a su código.
/// No lleva mensajes para la persona.
/// </summary>
public enum ProblemaDeResultado
{
    /// <summary>Goles fuera de 0 a 99, o un jugador repetido.</summary>
    DatosFueraDeRango,

    /// <summary>Una fila es de alguien que no está convocado.</summary>
    JugadorNoConvocado,

    /// <summary>Goles a quien no jugó.</summary>
    GolesDeQuienNoJugo,

    /// <summary>Los goles de los jugadores suman más que los del club.</summary>
    GolesSuperanMarcador,
}
