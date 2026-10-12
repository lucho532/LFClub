namespace LFClub.Infraestructura.Correo;

/// <summary>
/// Representa la configuración del envío de correos.
/// Su responsabilidad es llevar la llave y el remitente de Brevo (sección <c>Brevo</c>) y la
/// dirección del frontend con la que se arman los enlaces (<c>Frontend:UrlBase</c>).
/// No contiene la llave por defecto: es un secreto de cada entorno (constitución §6.3).
/// </summary>
public class OpcionesCorreo
{
    /// <summary>Llave de la API de Brevo. Vacía en desarrollo: los correos van al registro.</summary>
    public string Llave { get; set; } = string.Empty;

    /// <summary>Dirección de correo del remitente.</summary>
    public string Remitente { get; set; } = string.Empty;

    /// <summary>Dirección pública del frontend, sin barra final.</summary>
    public string UrlBaseFrontend { get; set; } = string.Empty;
}
