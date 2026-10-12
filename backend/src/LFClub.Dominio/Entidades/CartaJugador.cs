using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa lo que se guarda de la carta de un jugador en un club (constitución §14.3; spec 010,
/// RF-019 a RF-028).
/// Su responsabilidad es guardar el dorsal, la posición y las seis estadísticas que ponen quien
/// entrena su categoría y el PRESIDENTE (dorsal y posición también su familia), con el sello del
/// último cambio de cada parte y el nombre de su autor copiado en ese momento (§13). Hay como mucho
/// una por jugador y club, y nace con el primer cambio: un jugador sin fila tiene una carta sin
/// dorsal, sin posición y sin números.
/// No guarda la valoración ni la rareza, que se calculan al consultar; no se calcula de los
/// partidos ni de los entrenamientos ni los altera (RF-024); no guarda historial. Se conserva al
/// cambiar de categoría y al retirarse, y se borra en cascada con el integrante.
/// </summary>
public class CartaJugador : IPerteneceAClub
{
    /// <summary>Valor mínimo del dorsal y de cada estadística.</summary>
    public const int ValorMinimo = 1;

    /// <summary>Valor máximo del dorsal y de cada estadística.</summary>
    public const int ValorMaximo = 99;

    /// <summary>
    /// Clave primaria y foránea a <see cref="UsuarioRol"/>, borrado en cascada: una carta por
    /// jugador y club.
    /// </summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador dueño de la carta.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Dorsal, de 1 a 99. Opcional. Puede repetirse entre jugadores.</summary>
    public int? Dorsal { get; set; }

    /// <summary>Posición. Opcional.</summary>
    public PosicionJugador? Posicion { get; set; }

    /// <summary>Ritmo, de 1 a 99. Las seis estadísticas son nulas o tienen valor a la vez.</summary>
    public int? Ritmo { get; set; }

    /// <summary>Tiro, de 1 a 99.</summary>
    public int? Tiro { get; set; }

    /// <summary>Pase, de 1 a 99.</summary>
    public int? Pase { get; set; }

    /// <summary>Regate, de 1 a 99.</summary>
    public int? Regate { get; set; }

    /// <summary>Defensa, de 1 a 99.</summary>
    public int? Defensa { get; set; }

    /// <summary>Físico, de 1 a 99.</summary>
    public int? Fisico { get; set; }

    /// <summary>Último cambio de las seis estadísticas, en UTC; nulo si nunca se pusieron.</summary>
    public DateTime? EstadisticasCambiadasEn { get; set; }

    /// <summary>Cuenta que las cambió por última vez; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? EstadisticasCambiadasPorUsuarioId { get; set; }

    /// <summary>Nombres y apellidos de quien las cambió, copiados en ese momento (texto ≤ 161, §13).</summary>
    public string? EstadisticasCambiadasPorNombre { get; set; }

    /// <summary>Último cambio del dorsal o la posición, en UTC; nulo si nunca se pusieron.</summary>
    public DateTime? DorsalCambiadoEn { get; set; }

    /// <summary>Cuenta que los cambió por última vez; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? DorsalCambiadoPorUsuarioId { get; set; }

    /// <summary>
    /// Nombres y apellidos de quien los cambió, copiados en ese momento (texto ≤ 161, §13); el del
    /// jugador si fue su familia.
    /// </summary>
    public string? DorsalCambiadoPorNombre { get; set; }

    /// <summary>Si tiene las seis estadísticas puestas.</summary>
    public bool TieneEstadisticas => Ritmo is not null;
}
