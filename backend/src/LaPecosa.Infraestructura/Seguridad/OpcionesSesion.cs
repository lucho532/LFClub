namespace LaPecosa.Infraestructura.Seguridad;

/// <summary>
/// Representa la configuración de la sesión, leída de la sección <c>Sesion</c>.
/// Su responsabilidad es llevar la clave de firma de los tokens y su vigencia.
/// No contiene valores por defecto para la clave: es un secreto de cada entorno.
/// </summary>
public class OpcionesSesion
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string Seccion = "Sesion";

    /// <summary>Nombre de la reclamación del token que lleva el sello de seguridad.</summary>
    public const string ReclamacionSello = "sello";

    /// <summary>
    /// Nombre de la reclamación del token que lleva, una vez por cada uno, los integrantes a los
    /// que está limitada una sesión iniciada con un documento.
    /// </summary>
    public const string ReclamacionJugadores = "jugadores";

    /// <summary>Clave con la que se firman los tokens. Mínimo 32 caracteres.</summary>
    public string ClaveFirma { get; set; } = string.Empty;

    /// <summary>Días que dura una sesión (research §4, supuesto 1).</summary>
    public int DiasVigencia { get; set; } = 7;
}
