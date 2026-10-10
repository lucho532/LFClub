using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Dominio.Reglas;

/// <summary>
/// Representa el resultado de decidir cuál de los integrantes de una cuenta en un club hace una
/// petición (<see cref="ReglaJugadorDeLaSesion"/>).
/// Su responsabilidad es distinguir tres casos: hay un integrante y es ese; no hay ninguno al que
/// la petición pueda referirse; o la cuenta tiene varios y todavía no eligió.
/// No contiene el mensaje ni el código de error que ve la persona, y no dice si ese integrante
/// puede entrar al club: eso lo decide después <see cref="ReglaAccesoPorEstado"/>.
/// </summary>
public sealed class JugadorDeLaPeticion
{
    private JugadorDeLaPeticion(UsuarioRol? integrante, bool sinElegir)
    {
        Integrante = integrante;
        SinElegir = sinElegir;
    }

    /// <summary>
    /// La petición no se refiere a ningún integrante de la cuenta en el club: se responde como si
    /// no existiera.
    /// </summary>
    public static JugadorDeLaPeticion NoEncontrado { get; } = new(null, false);

    /// <summary>La cuenta tiene varios integrantes en el club y la petición no dice cuál.</summary>
    public static JugadorDeLaPeticion PorElegir { get; } = new(null, true);

    /// <summary>El integrante que hace la petición; nulo en los otros dos casos.</summary>
    public UsuarioRol? Integrante { get; }

    /// <summary>Indica que la cuenta tiene que elegir con cuál de sus integrantes continúa.</summary>
    public bool SinElegir { get; }

    /// <summary>El resultado cuando quien hace la petición es ese integrante.</summary>
    public static JugadorDeLaPeticion Es(UsuarioRol integrante) => new(integrante, false);
}
