using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa lo que la familia de un jugador respondió ante un entrenamiento de su categoría
/// (spec 009, RF-023).
/// Su responsabilidad es guardar la respuesta y cuándo se dio. La escribe solo la cuenta del
/// jugador, mientras el entrenamiento esté programado y no haya empezado.
/// No es la asistencia real (RF-025). Se borra con el jugador y, si se retira, la de los
/// entrenamientos que todavía no empiezan (RF-033).
/// </summary>
public class RespuestaEntrenamiento : IPerteneceAClub
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

    /// <summary>Asistirá o no asistirá.</summary>
    public RespuestaFamilia Respuesta { get; set; }

    /// <summary>Última vez que respondió, en UTC.</summary>
    public DateTime RespondidaEn { get; set; }
}
