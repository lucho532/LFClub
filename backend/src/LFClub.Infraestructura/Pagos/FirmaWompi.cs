using System.Security.Cryptography;
using System.Text;

namespace LFClub.Infraestructura.Pagos;

/// <summary>
/// Representa las firmas de Wompi (spec 017, research §2).
/// Su responsabilidad es calcular la firma de integridad de la dirección de pago
/// (SHA-256 de referencia + valor en centavos + moneda + secreto de integridad) y el checksum de un
/// evento (SHA-256 de los valores de las propiedades firmadas + marca de tiempo + secreto de
/// eventos), y compararlos en tiempo constante. Formato según la documentación de Wompi:
/// https://docs.wompi.co/docs/colombia/widget-checkout-web/ (integridad) y
/// https://docs.wompi.co/docs/colombia/eventos/ (eventos).
/// No conoce HTTP ni guarda secretos.
/// </summary>
public static class FirmaWompi
{
    /// <summary>Firma de integridad de la dirección de pago, en hexadecimal minúscula.</summary>
    public static string Integridad(string referencia, long centavos, string moneda, string secreto) =>
        Sha256Hex($"{referencia}{centavos}{moneda}{secreto}");

    /// <summary>Checksum de un evento, en hexadecimal minúscula.</summary>
    public static string ChecksumDeEvento(IEnumerable<string> valoresDeLasPropiedades, string marcaDeTiempo, string secreto) =>
        Sha256Hex(string.Concat(valoresDeLasPropiedades) + marcaDeTiempo + secreto);

    /// <summary>Compara dos firmas sin importar mayúsculas y en tiempo constante.</summary>
    public static bool SonIguales(string esperada, string recibida)
    {
        var a = Encoding.UTF8.GetBytes(esperada.ToLowerInvariant());
        var b = Encoding.UTF8.GetBytes(recibida.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Sha256Hex(string texto) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
}
