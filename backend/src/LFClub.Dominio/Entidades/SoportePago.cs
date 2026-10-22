namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa la imagen que la familia sube como comprobante de una transferencia (spec 015, RF-020).
/// Su responsabilidad es guardar los bytes y su tipo de contenido, uno por pago. Nunca tiene
/// dirección pública: solo la familia del jugador, el PRESIDENTE y el DIRECTIVO la abren. Se borra a
/// mano al eliminar la cuenta del jugador (spec 014) y en cascada con el pago y el club.
/// No valida el formato (lo hace quien la sube) ni guarda el nombre original del archivo.
/// </summary>
public class SoportePago : IPerteneceAClub
{
    /// <summary>Pago al que pertenece. Clave primaria; borrado en cascada.</summary>
    public Guid PagoId { get; set; }

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Bytes de la imagen. Obligatorio, hasta 5 MB.</summary>
    public byte[] Contenido { get; set; } = [];

    /// <summary>Máx. 20; <c>image/png</c>, <c>image/jpeg</c> o <c>image/webp</c>, según la firma.</summary>
    public string TipoContenido { get; set; } = string.Empty;

    /// <summary>Fecha y hora UTC en que se subió.</summary>
    public DateTime GuardadaEn { get; set; }
}
