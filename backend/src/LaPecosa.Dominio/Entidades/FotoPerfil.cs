namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la foto de perfil de una cuenta (RF-036 a RF-038).
/// Su responsabilidad es guardar los bytes y el tipo de contenido, separados de
/// <see cref="Usuario"/> para no cargarlos en cada consulta. Es opcional y la misma en todos los
/// clubes de la persona; se borra con la cuenta.
/// No pertenece a ningún club. La ve su dueño y, si la cuenta es PRESIDENTE, DIRECTIVO o
/// ENTRENADOR en un club, los integrantes de ese club, solo en su ficha básica y en la miniatura
/// junto a su nombre (§14.2 4.5.0, spec 008). La de una cuenta que en un club es JUGADOR no la ve
/// nadie de ese club por esa vía. Solo su dueño la carga, la cambia o la quita.
/// </summary>
public class FotoPerfil
{
    /// <summary>Cuenta dueña de la foto. Es también la clave primaria: una foto por cuenta.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Cuenta dueña de la foto.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Bytes de la imagen. Máximo 1 MB.</summary>
    public byte[] Contenido { get; set; } = [];

    /// <summary>
    /// <c>image/png</c>, <c>image/jpeg</c> o <c>image/webp</c>, según la firma del archivo.
    /// </summary>
    public string TipoContenido { get; set; } = string.Empty;
}
