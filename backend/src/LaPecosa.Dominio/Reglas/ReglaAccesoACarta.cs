using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla de lo que cada uno puede hacer con la carta de un jugador que ya ve
/// (constitución §14.3; spec 010, RF-021, RF-027, RF-029 y RF-031; research §8 y §12.2).
/// Su responsabilidad es resolver, en un único lugar, la tabla de permisos: el PRESIDENTE cambia
/// estadísticas, dorsal y posición de un jugador activo y exporta siempre, también la de un
/// retirado; quien entrena la categoría activa del jugador (ENTRENADOR o DIRECTIVO asignados)
/// cambia estadísticas, dorsal y posición y no exporta; la cuenta del jugador, sobre su propio
/// jugador, cambia dorsal y posición y exporta, y nunca las estadísticas; los demás solo miran.
/// No decide quién ve la carta: eso es <c>AccesoAFicha</c>. No consulta la base de datos: recibe
/// resuelto si es su propio jugador, si entrena su categoría activa y si está activo.
/// </summary>
public static class ReglaAccesoACarta
{
    /// <summary>Lo que quien pregunta puede hacer con la carta.</summary>
    public static AlcanceDeCarta Alcance(Rol rol, bool esSuPropioJugador, bool entrenaSuCategoriaActiva, bool jugadorActivo) =>
        rol switch
        {
            Rol.PRESIDENTE => new AlcanceDeCarta(jugadorActivo, jugadorActivo, true),
            Rol.ENTRENADOR or Rol.DIRECTIVO when entrenaSuCategoriaActiva && jugadorActivo => new AlcanceDeCarta(true, true, false),
            Rol.JUGADOR when esSuPropioJugador => new AlcanceDeCarta(false, jugadorActivo, true),
            _ => AlcanceDeCarta.Ninguno,
        };
}
