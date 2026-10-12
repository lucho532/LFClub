using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién puede publicar un comunicado y a qué audiencia (constitución §14.4;
/// spec 011, RF-003 a RF-007; research §4).
/// Su responsabilidad es decidir, con el rol, el alcance pedido y las categorías, si la audiencia es
/// coherente y si quien publica puede dirigirse a ella: el PRESIDENTE y el DIRECTIVO, a cualquiera;
/// el ENTRENADOR, a categorías o equipos de las categorías que entrena y nunca a todo el club; el
/// JUGADOR y el DESARROLLADOR, a ninguna. Un PRESIDENTE o DIRECTIVO asignado como entrenador se
/// evalúa con el alcance de su rol (RF-006).
/// No comprueba que las categorías o los equipos existan, estén activos o sean del club, ni que
/// haya destinatarios: eso lo hace el servicio con datos.
/// </summary>
public static class ReglaAudienciaDeComunicado
{
    /// <summary>
    /// Evalúa la audiencia pedida.
    /// </summary>
    /// <param name="rol">Rol de quien publica.</param>
    /// <param name="alcance">Alcance pedido.</param>
    /// <param name="categoriasSolicitadas">Categorías pedidas (solo con <c>CATEGORIAS</c>).</param>
    /// <param name="categoriasDelEntrenador">Categorías activas que entrena quien publica.</param>
    /// <param name="categoriasDeLosEquipos">Categoría de cada equipo pedido, una por equipo (solo con <c>EQUIPOS</c>).</param>
    public static ResultadoAudiencia Validar(
        Rol rol,
        AlcanceComunicado alcance,
        IReadOnlyCollection<Guid> categoriasSolicitadas,
        IReadOnlyCollection<Guid> categoriasDelEntrenador,
        IReadOnlyCollection<Guid> categoriasDeLosEquipos)
    {
        if (rol is not (Rol.PRESIDENTE or Rol.DIRECTIVO or Rol.ENTRENADOR))
        {
            return ResultadoAudiencia.RolNoPublica;
        }

        var equiposSolicitados = categoriasDeLosEquipos.Count;
        var categoriasDeEquipos = categoriasDeLosEquipos.Distinct().ToList();
        var coherente = alcance switch
        {
            AlcanceComunicado.CLUB => categoriasSolicitadas.Count == 0 && equiposSolicitados == 0,
            AlcanceComunicado.CATEGORIAS => categoriasSolicitadas.Count > 0 && equiposSolicitados == 0,
            AlcanceComunicado.EQUIPOS => equiposSolicitados > 0 && categoriasSolicitadas.Count == 0
                && categoriasDeEquipos.Count <= 1,
            _ => false,
        };
        if (!coherente)
        {
            return ResultadoAudiencia.AudienciaInvalida;
        }

        if (rol is Rol.PRESIDENTE or Rol.DIRECTIVO)
        {
            return ResultadoAudiencia.Permitida;
        }

        if (alcance == AlcanceComunicado.CLUB)
        {
            return ResultadoAudiencia.FueraDeSuAlcance;
        }

        var pedidas = alcance == AlcanceComunicado.CATEGORIAS ? categoriasSolicitadas : categoriasDeEquipos;
        return pedidas.All(categoriasDelEntrenador.Contains)
            ? ResultadoAudiencia.Permitida
            : ResultadoAudiencia.FueraDeSuAlcance;
    }
}

/// <summary>
/// Representa el resultado de evaluar una audiencia de comunicado.
/// Su responsabilidad es nombrar los cuatro desenlaces, cada uno con su error del contrato.
/// No lleva datos.
/// </summary>
public enum ResultadoAudiencia
{
    /// <summary>Puede publicar a esa audiencia.</summary>
    Permitida,

    /// <summary>Entrena otras categorías o pidió todo el club (403 <c>no_entrena_categoria</c>).</summary>
    FueraDeSuAlcance,

    /// <summary>Su rol nunca publica (403 <c>rol_no_autorizado</c>).</summary>
    RolNoPublica,

    /// <summary>Alcance y destinos incoherentes (400 <c>audiencia_invalida</c>).</summary>
    AudienciaInvalida,
}
