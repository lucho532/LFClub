namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa las direcciones de la aplicación web que el servidor necesita escribir en lo que envía
/// fuera de ella, como la dirección a la que vuelve la persona después de pagar (spec 017).
/// Su responsabilidad es llevar la dirección base del frontend, sin barra final, tomada de
/// <c>Frontend:UrlBase</c>.
/// No contiene secretos.
/// </summary>
public class OpcionesDeEnlaces
{
    /// <summary>Dirección base del frontend, sin barra final.</summary>
    public string UrlBaseFrontend { get; set; } = string.Empty;
}
