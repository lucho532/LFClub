using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa el archivo entregado para uno de los documentos que pide la ficha de un jugador
/// (RF-027 a RF-029).
/// Su responsabilidad es guardar los bytes, su tipo de contenido, su tamaño y la fecha en que se
/// subió. Como mucho hay uno por jugador y documento pedido: subir otro sustituye a este, que deja
/// de existir. Que la fila exista es lo que significa "entregado".
/// No guarda el nombre original del archivo, versiones anteriores ni un estado de revisión, y no
/// valida el formato: eso lo hace quien lo sube.
/// </summary>
public class DocumentoJugador : IPerteneceAClub
{
    /// <summary>Jugador. Parte de la clave primaria.</summary>
    public Guid UsuarioRolId { get; set; }

    /// <summary>Jugador.</summary>
    public UsuarioRol? UsuarioRol { get; set; }

    /// <summary>Documento pedido. Parte de la clave primaria; se guarda como texto.</summary>
    public DocumentoPedido Documento { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece; siempre el del jugador.</summary>
    public Club? Club { get; set; }

    /// <summary>Bytes del archivo. Obligatorio, máximo 10 MB.</summary>
    public byte[] Contenido { get; set; } = [];

    /// <summary>
    /// Máx. 30; <c>application/pdf</c>, <c>image/jpeg</c>, <c>image/png</c> o <c>image/webp</c>,
    /// según la firma del archivo.
    /// </summary>
    public string TipoContenido { get; set; } = string.Empty;

    /// <summary>Tamaño en bytes. Obligatorio; permite mostrarlo sin leer el contenido.</summary>
    public int TamanoBytes { get; set; }

    /// <summary>Fecha y hora UTC en que se subió el archivo vigente. Obligatorio (RF-028).</summary>
    public DateTime SubidoEn { get; set; }
}
