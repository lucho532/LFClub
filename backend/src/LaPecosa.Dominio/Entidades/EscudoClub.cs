namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la imagen del escudo de un club.
/// Su responsabilidad es guardar los bytes y el tipo de contenido, separados de <see cref="Club"/>
/// para no cargarlos en cada consulta (research §10).
/// Lo cargan el DESARROLLADOR desde el panel y el PRESIDENTE del club desde "Datos del club".
/// No guarda la versión del escudo (va en el club) ni valida la imagen.
/// </summary>
public class EscudoClub : IPerteneceAClub
{
    /// <summary>Club dueño del escudo. Es también la clave primaria: un escudo por club.</summary>
    public Guid ClubId { get; set; }

    /// <summary>Club dueño del escudo.</summary>
    public Club? Club { get; set; }

    /// <summary>Bytes de la imagen. Máximo 1 MB.</summary>
    public byte[] Contenido { get; set; } = [];

    /// <summary>
    /// <c>image/png</c>, <c>image/jpeg</c> o <c>image/webp</c>, según la firma del archivo.
    /// </summary>
    public string TipoContenido { get; set; } = string.Empty;
}
