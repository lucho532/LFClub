namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la foto propia de un jugador en un club (constitución §14.2).
/// Su responsabilidad es guardar los bytes, el tipo de contenido y la fecha de la última carga,
/// separados de <see cref="UsuarioRol"/> y de <see cref="FichaJugador"/> para no cargarlos en cada
/// consulta. Hay como mucho una por jugador y club; la cargan la cuenta del jugador y el
/// PRESIDENTE.
/// Es independiente de <see cref="FotoPerfil"/>, que es de la cuenta y no la ve el club. No tiene
/// estado <c>Activo</c> ni historial: se borra físicamente al quitarla y cuando el jugador deja el
/// club (lo retira el PRESIDENTE, lo retira su familia o queda retirado al aprobarse un hermano en
/// otro club), excepción expresa de §14.2 a la conservación de los datos del retirado. El borrado
/// por cambio de rol (§12.2) queda para la funcionalidad que construya ese cambio. No valida la
/// imagen.
/// </summary>
public class FotoJugador : IPerteneceAClub
{
    /// <summary>
    /// Clave primaria y foránea a <see cref="UsuarioRol"/>, borrado en cascada. Una foto por
    /// jugador y club.
    /// </summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador dueño de la foto.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <summary>Foránea a <see cref="Club"/>, borrado en cascada. Siempre el del jugador.</summary>
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece; siempre el del jugador.</summary>
    public Club? Club { get; set; }

    /// <summary>Bytes de la imagen. Obligatorio. Máximo 1 MB.</summary>
    public byte[] Contenido { get; set; } = [];

    /// <summary>
    /// Máx. 20, obligatorio. <c>image/png</c>, <c>image/jpeg</c> o <c>image/webp</c>, según la
    /// firma del archivo, nunca la extensión.
    /// </summary>
    public string TipoContenido { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC de la última carga. Obligatorio. Se reescribe en cada carga; es lo que la
    /// API entrega como <c>fotoActualizadaEn</c>.
    /// </summary>
    public DateTime GuardadaEn { get; set; }
}
