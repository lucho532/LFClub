namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa el motivo por el que se rechaza una imagen cargada.
/// Su responsabilidad es distinguir los dos rechazos del contrato.
/// No contiene el código de error: escudo y foto de perfil usan códigos distintos.
/// </summary>
public enum MotivoRechazoImagen
{
    /// <summary>El archivo no es PNG, JPEG ni WebP.</summary>
    NoEsImagen,

    /// <summary>El archivo supera 1 MB.</summary>
    DemasiadoGrande,
}
