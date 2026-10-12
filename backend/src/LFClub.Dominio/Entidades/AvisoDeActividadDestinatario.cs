namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa que un jugador activo recibió un aviso de actividad al generarse (spec 012, RF-009 a
/// RF-011 y RF-018 a RF-024).
/// Su responsabilidad es guardar la pareja aviso y jugador, la cuenta dueña del jugador y cuándo esa
/// cuenta lo leyó. La audiencia es una foto del momento de generar el aviso: nunca se crean filas
/// después, ni se borran al retirar al jugador o al cambiarlo de categoría.
/// No decide si la fila cuenta hoy (solo cuenta si su jugador sigue activo) ni guarda el estado de
/// lectura por jugador: <see cref="LeidoEn"/> es de la cuenta y vale igual en todas sus filas del
/// mismo aviso.
/// </summary>
public class AvisoDeActividadDestinatario : IPerteneceAClub
{
    /// <summary>Aviso. Parte de la clave primaria.</summary>
    public Guid AvisoId { get; set; }

    /// <summary>Aviso.</summary>
    public AvisoDeActividad? Aviso { get; set; }

    /// <summary>Jugador que lo recibió. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador que lo recibió.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Cuenta dueña del jugador; la que lee y la que cuenta.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Fecha y hora en que la cuenta lo leyó, en UTC. Nulo mientras no lo lea.</summary>
    public DateTime? LeidoEn { get; set; }
}
