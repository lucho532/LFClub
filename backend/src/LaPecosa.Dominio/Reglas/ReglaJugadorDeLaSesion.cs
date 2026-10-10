using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa la regla que decide cuál de los integrantes de una cuenta en un club hace una
/// petición (constitución §8 y §12.4; RF-021 a RF-029 de la 006).
/// Su responsabilidad es resolverlo en un único lugar. Una cuenta con un solo integrante en el
/// club es siempre ese. Una cuenta con varios (hermanos) dice cuál con el jugador elegido; si no
/// lo dice, tiene que elegir. Y una sesión iniciada con el documento de un jugador está limitada a
/// él: en un club con varios integrantes, el de la petición es el de la limitación y el jugador
/// elegido no puede cambiarlo. Un identificador que no es de la cuenta en el club se trata como si
/// no existiera.
/// No conoce HTTP ni la base de datos: recibe ya leídos los integrantes, el jugador elegido y la
/// limitación. No mira el estado de ingreso, el retiro ni el rol del integrante: eso lo aplica
/// después <see cref="ReglaAccesoPorEstado"/>.
/// </summary>
public static class ReglaJugadorDeLaSesion
{
    /// <summary>
    /// Resuelve quién hace la petición.
    /// <paramref name="integrantesDeLaCuentaEnElClub"/> son todos los de la cuenta en el club de
    /// la petición; <paramref name="jugadorElegido"/>, el identificador que envía la petición, o
    /// nulo si no envía ninguno; <paramref name="limitacion"/>, los integrantes a los que está
    /// limitada la sesión, o nulo si no lo está.
    /// </summary>
    public static JugadorDeLaPeticion Resolver(
        IReadOnlyList<UsuarioRol> integrantesDeLaCuentaEnElClub,
        Guid? jugadorElegido,
        IReadOnlyCollection<Guid>? limitacion)
    {
        if (integrantesDeLaCuentaEnElClub.Count == 0)
        {
            return JugadorDeLaPeticion.NoEncontrado;
        }

        if (integrantesDeLaCuentaEnElClub.Count == 1)
        {
            // Sin hermanos en el club no hay nada que elegir ni que limitar.
            return ElegidoO(integrantesDeLaCuentaEnElClub[0], jugadorElegido);
        }

        if (limitacion is not null)
        {
            var limitado = integrantesDeLaCuentaEnElClub.FirstOrDefault(integrante => limitacion.Contains(integrante.Id));
            return limitado is null ? JugadorDeLaPeticion.NoEncontrado : ElegidoO(limitado, jugadorElegido);
        }

        if (jugadorElegido is null)
        {
            return JugadorDeLaPeticion.PorElegir;
        }

        var elegido = integrantesDeLaCuentaEnElClub.FirstOrDefault(integrante => integrante.Id == jugadorElegido);
        return elegido is null ? JugadorDeLaPeticion.NoEncontrado : JugadorDeLaPeticion.Es(elegido);
    }

    /// <summary>
    /// Ese integrante, salvo que la petición elija a otro: elegir a otro no lo cambia, responde
    /// como si no existiera.
    /// </summary>
    private static JugadorDeLaPeticion ElegidoO(UsuarioRol integrante, Guid? jugadorElegido) =>
        jugadorElegido is null || jugadorElegido == integrante.Id
            ? JugadorDeLaPeticion.Es(integrante)
            : JugadorDeLaPeticion.NoEncontrado;
}
