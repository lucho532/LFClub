namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa cada uno de los documentos que la ficha de un jugador pide a su familia (RF-027).
/// Su responsabilidad es nombrar esa lista, que es fija, la misma para todos los clubes y no se
/// amplía desde la aplicación.
/// No es el tipo del documento de identidad: eso es <see cref="TipoDocumento"/>. No dice si el
/// documento está entregado ni guarda el archivo.
/// </summary>
public enum DocumentoPedido
{
    /// <summary>Copia del documento de identidad del jugador.</summary>
    COPIA_DOCUMENTO_IDENTIDAD,

    /// <summary>Certificado de afiliación a salud.</summary>
    CERTIFICADO_SALUD,
}
