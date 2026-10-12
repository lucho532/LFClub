using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de lo que la familia de un convocado puede publicar o indicar en un partido
/// (spec 010, RF-001 a RF-012; research §5).
/// Su responsabilidad es revisar el estado del partido, si ya empezó, el marcador actual y el
/// enviado, el origen de la fila del jugador, lo que la familia indica y la suma de goles de los
/// demás convocados, y devolver el primer problema en el orden de data-model: cancelado, sin
/// empezar, falta el marcador, el marcador ya está publicado (aunque se envíe igual), la fila ya la
/// corrigió quien entrena, datos fuera de rango, goles de quien no jugó y goles que superan el
/// marcador.
/// No consulta la base de datos, no sabe quién es el jugador ni si está convocado, no guarda nada y
/// nunca devuelve los goles de los demás: el problema no nombra a nadie (RF-006).
/// </summary>
public static class ReglaResultadoDeFamilia
{
    /// <summary>
    /// El primer problema, o nulo si es válido. <paramref name="golesClubActual"/> y
    /// <paramref name="golesRivalActual"/> son el marcador guardado (nulos si no hay);
    /// <paramref name="golesClubEnviado"/> y <paramref name="golesRivalEnviado"/>, el que envía la
    /// familia (nulos si no lo envía); <paramref name="golesDeLosDemas"/>, la suma de goles de los
    /// demás convocados.
    /// </summary>
    public static ProblemaDeResultadoDeFamilia? Validar(
        EstadoPartido estado,
        bool yaEmpezo,
        int? golesClubActual,
        int? golesRivalActual,
        int? golesClubEnviado,
        int? golesRivalEnviado,
        OrigenResultado? origenDeLaFila,
        bool jugo,
        int goles,
        int golesDeLosDemas)
    {
        if (estado == EstadoPartido.CANCELADO)
        {
            return ProblemaDeResultadoDeFamilia.PartidoCancelado;
        }

        if (estado == EstadoPartido.PROXIMO && !yaEmpezo)
        {
            return ProblemaDeResultadoDeFamilia.PartidoSinEmpezar;
        }

        var tieneMarcador = golesClubActual is not null && golesRivalActual is not null;
        if (!tieneMarcador && (golesClubEnviado is null || golesRivalEnviado is null))
        {
            return ProblemaDeResultadoDeFamilia.FaltaMarcador;
        }

        if (tieneMarcador && (golesClubEnviado is not null || golesRivalEnviado is not null))
        {
            return ProblemaDeResultadoDeFamilia.MarcadorYaPublicado;
        }

        if (origenDeLaFila == OrigenResultado.GESTION)
        {
            return ProblemaDeResultadoDeFamilia.IndicacionCorregida;
        }

        var golesClub = tieneMarcador ? golesClubActual!.Value : golesClubEnviado!.Value;
        if (!EnRango(goles) || !EnRango(golesClub) || (!tieneMarcador && !EnRango(golesRivalEnviado!.Value)))
        {
            return ProblemaDeResultadoDeFamilia.DatosFueraDeRango;
        }

        if (!jugo && goles > 0)
        {
            return ProblemaDeResultadoDeFamilia.GolesDeQuienNoJugo;
        }

        return goles + golesDeLosDemas > golesClub ? ProblemaDeResultadoDeFamilia.GolesSuperanMarcador : null;
    }

    private static bool EnRango(int goles) => goles is >= 0 and <= Partido.MaximoDeGoles;
}

/// <summary>
/// Representa por qué no se acepta lo que envía la familia de un convocado (spec 010).
/// Su responsabilidad es nombrar cada problema para que la aplicación lo traduzca a su código.
/// No lleva mensajes para la persona ni datos de otros convocados.
/// </summary>
public enum ProblemaDeResultadoDeFamilia
{
    /// <summary>El partido está cancelado.</summary>
    PartidoCancelado,

    /// <summary>El partido todavía no empieza.</summary>
    PartidoSinEmpezar,

    /// <summary>El partido no tiene marcador y no se envía entero.</summary>
    FaltaMarcador,

    /// <summary>El partido ya tiene marcador y se envía otro, aunque sea igual.</summary>
    MarcadorYaPublicado,

    /// <summary>Quien entrena o el PRESIDENTE ya corrigió lo del jugador.</summary>
    IndicacionCorregida,

    /// <summary>Goles o marcador fuera de 0 a 99.</summary>
    DatosFueraDeRango,

    /// <summary>Goles a quien no jugó.</summary>
    GolesDeQuienNoJugo,

    /// <summary>Los goles del jugador y de los demás suman más que los del club.</summary>
    GolesSuperanMarcador,
}
