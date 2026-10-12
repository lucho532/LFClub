using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién retira un comunicado (constitución §14.4; spec 011, RF-025 a RF-027;
/// research §7 y §8).
/// Su responsabilidad es decidirlo con hechos ya resueltos: lo retira quien lo publicó y el
/// PRESIDENTE. Un comunicado ya retirado no se retira otra vez; al ENTRENADOR que no es el autor el
/// comunicado no existe (no lo ve); al DIRECTIVO que no es el autor sí lo ve y no lo retira. Un
/// comunicado sin autor (dejó el club) solo lo retira el PRESIDENTE.
/// No consulta la base de datos, no distingue el rol con el que lo publicó el autor y no decide
/// quién lo ve en las listas (eso es de <see cref="ReglaVisibilidadDeComunicadosPublicados"/>).
/// </summary>
public static class ReglaRetiroDeComunicado
{
    /// <summary>Evalúa el retiro de un comunicado por quien pregunta.</summary>
    /// <param name="rol">Rol de quien pregunta.</param>
    /// <param name="integranteId">Integrante de la petición.</param>
    /// <param name="autorId">Integrante que lo publicó, o nulo si ya no está.</param>
    /// <param name="retirado">Si el comunicado ya está retirado.</param>
    public static ResultadoRetiroDeComunicado Evaluar(Rol rol, Guid integranteId, Guid? autorId, bool retirado)
    {
        if (!ReglaVisibilidadDeComunicadosPublicados.Ve(rol, integranteId, autorId))
        {
            return ResultadoRetiroDeComunicado.NoLoVe;
        }

        var esAutor = autorId == integranteId;
        if (rol != Rol.PRESIDENTE && !esAutor)
        {
            return ResultadoRetiroDeComunicado.NoRetira;
        }

        return retirado ? ResultadoRetiroDeComunicado.YaRetirado : ResultadoRetiroDeComunicado.Retira;
    }
}

/// <summary>
/// Representa el resultado de evaluar el retiro de un comunicado.
/// Su responsabilidad es nombrar los cuatro desenlaces, cada uno con su error del contrato.
/// No lleva datos.
/// </summary>
public enum ResultadoRetiroDeComunicado
{
    /// <summary>Puede retirarlo.</summary>
    Retira,

    /// <summary>Ya estaba retirado (409 <c>comunicado_retirado</c>).</summary>
    YaRetirado,

    /// <summary>No lo ve: para él no existe (404).</summary>
    NoLoVe,

    /// <summary>Lo ve y no lo retira (403 <c>no_retira_comunicado</c>).</summary>
    NoRetira,
}
