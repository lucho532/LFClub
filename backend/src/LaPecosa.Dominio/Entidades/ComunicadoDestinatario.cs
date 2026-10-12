namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa que un jugador activo recibió un comunicado al publicarse (spec 011, RF-009 y
/// RF-012 a RF-023).
/// Su responsabilidad es guardar la pareja comunicado y jugador, la cuenta dueña del jugador y
/// cuándo esa cuenta lo leyó. La audiencia es una foto del momento de publicar: nunca se crean
/// filas después, ni se borran al retirar al jugador o al cambiarlo de categoría.
/// No decide si la fila cuenta hoy (solo cuenta si su jugador sigue activo y el comunicado no está
/// retirado) ni guarda el estado de lectura por jugador: <see cref="LeidoEn"/> es de la cuenta y
/// vale igual en todas sus filas del mismo comunicado.
/// </summary>
public class ComunicadoDestinatario : IPerteneceAClub
{
    /// <summary>Comunicado. Parte de la clave primaria.</summary>
    public Guid ComunicadoId { get; set; }

    /// <summary>Comunicado.</summary>
    public Comunicado? Comunicado { get; set; }

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
