using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de quién ve y quién opera la cuenta de un jugador (spec 015, RF-035;
/// constitución §16.9).
/// Su responsabilidad es decidir, por rol: el PRESIDENTE y el DIRECTIVO ven la cuenta de cualquier
/// jugador del club, también retirado; la cuenta de un jugador ve solo la del jugador que es, y no si
/// está retirado; solo el PRESIDENTE registra, confirma, rechaza y anula; la cuenta del jugador informa
/// transferencias de su propio jugador. El ENTRENADOR y el DESARROLLADOR no ven nada de esto.
/// No comprueba que el jugador sea del club (eso lo hace la consulta con el filtro de aislamiento) ni
/// el estado de ingreso de quien pregunta (eso lo hace <c>IntegranteDelClub</c>).
/// </summary>
public static class ReglaAccesoACuenta
{
    /// <summary>Indica si ese rol ve las finanzas del club, los pagos en revisión y la mensualidad.</summary>
    public static bool VeFinanzasDelClub(Rol rol) => rol is Rol.PRESIDENTE or Rol.DIRECTIVO;

    /// <summary>
    /// Indica si quien pregunta puede ver la cuenta de ese jugador.
    /// </summary>
    /// <param name="rolDeQuienPregunta">Rol de quien pregunta en el club.</param>
    /// <param name="esElMismoJugador">Si quien pregunta es el propio jugador.</param>
    /// <param name="jugadorActivo">Si el jugador consultado no está retirado.</param>
    public static bool PuedeVer(Rol rolDeQuienPregunta, bool esElMismoJugador, bool jugadorActivo) =>
        rolDeQuienPregunta switch
        {
            Rol.PRESIDENTE or Rol.DIRECTIVO => true,
            Rol.JUGADOR => esElMismoJugador && jugadorActivo,
            _ => false,
        };

    /// <summary>Indica si ese rol registra, confirma, rechaza o anula pagos y fija valores.</summary>
    public static bool PuedeOperar(Rol rol) => rol == Rol.PRESIDENTE;

    /// <summary>Indica si ese rol informa transferencias, solo sobre su propio jugador.</summary>
    public static bool PuedeInformar(Rol rol, bool esElMismoJugador) => rol == Rol.JUGADOR && esElMismoJugador;
}
