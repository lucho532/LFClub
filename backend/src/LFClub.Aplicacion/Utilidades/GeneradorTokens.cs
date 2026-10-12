using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el generador de los tokens de un solo uso de las invitaciones y de la recuperación
/// de contraseña.
/// Su responsabilidad es crear tokens aleatorios y calcular el hash con el que se guardan.
/// No guarda tokens ni decide su vigencia; el token en claro solo viaja en el enlace del correo.
/// </summary>
public static class GeneradorTokens
{
    /// <summary>Crea un token de 32 bytes aleatorios, en base64url, junto con su hash.</summary>
    public static (string Token, string Hash) Nuevo()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    /// <summary>SHA-256 del token, en hexadecimal en minúsculas (64 caracteres).</summary>
    public static string Hash(string? token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));
}
