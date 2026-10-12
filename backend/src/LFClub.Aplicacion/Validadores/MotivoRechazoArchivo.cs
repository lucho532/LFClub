namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa el motivo por el que se rechaza un archivo de la ficha de un jugador.
/// Su responsabilidad es distinguir los dos rechazos del contrato.
/// No contiene el código ni el texto del error: eso lo decide quien lo traduce.
/// </summary>
public enum MotivoRechazoArchivo
{
    /// <summary>El archivo no es un PDF ni una imagen JPEG, PNG o WebP, o está vacío.</summary>
    NoAdmitido,

    /// <summary>El archivo supera los 10 MB.</summary>
    DemasiadoGrande,
}
