using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa un jugador que ve un entrenamiento sin ser de su audiencia natural: un añadido por
/// quien dirige o uno congelado al desactivar el equipo (spec 013, RF-013 y RF-026).
/// Su responsabilidad es guardar la pareja entrenamiento y jugador, de dónde viene y cuándo entró.
/// No cambia la categoría ni los equipos del jugador (RF-015) y quitarlo borra la fila sin dejar
/// rastro (RF-017).
/// </summary>
public class EntrenamientoJugador : IPerteneceAClub
{
    /// <summary>Entrenamiento. Parte de la clave primaria.</summary>
    public Guid EntrenamientoId { get; set; }

    /// <summary>Entrenamiento.</summary>
    public Entrenamiento? Entrenamiento { get; set; }

    /// <summary>Jugador. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>De dónde viene la fila.</summary>
    public OrigenDeJugadorEnEntrenamiento Origen { get; set; }

    /// <summary>
    /// Equipo desactivado del que se copió al jugador; solo con
    /// <see cref="OrigenDeJugadorEnEntrenamiento.EQUIPO_DESACTIVADO"/>. Pasa a nulo si el equipo se
    /// borra.
    /// </summary>
    public Guid? EquipoDeOrigenId { get; set; }

    /// <summary>Equipo del que se copió.</summary>
    public Equipo? EquipoDeOrigen { get; set; }

    /// <summary>Fecha y hora en que entró a la lista, en UTC.</summary>
    public DateTime AnadidoEn { get; set; }
}
