using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de qué comunicados publicados ve el equipo del club (constitución §14.4;
/// spec 011, RF-024; research §8).
/// Su responsabilidad es decidirlo con el rol: el PRESIDENTE y el DIRECTIVO ven todos los del club,
/// retirados incluidos; el ENTRENADOR, solo los que él publicó; el JUGADOR y el DESARROLLADOR, ninguno
/// (la cuenta de un jugador ve solo lo que le llegó, que es otra consulta).
/// No consulta la base de datos. El repositorio traduce esta misma condición a SQL en la lista.
/// </summary>
public static class ReglaVisibilidadDeComunicadosPublicados
{
    /// <summary>Indica si quien pregunta ve el comunicado publicado.</summary>
    /// <param name="rol">Rol de quien pregunta.</param>
    /// <param name="integranteId">Integrante de la petición.</param>
    /// <param name="autorId">Integrante que lo publicó, o nulo si ya no está.</param>
    public static bool Ve(Rol rol, Guid integranteId, Guid? autorId) => rol switch
    {
        Rol.PRESIDENTE or Rol.DIRECTIVO => true,
        Rol.ENTRENADOR => autorId == integranteId,
        _ => false,
    };
}
