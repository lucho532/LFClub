using LFClub.Aplicacion.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LFClub.Infraestructura.Seguridad;

/// <summary>
/// Representa el hash de contraseñas con el <c>PasswordHasher</c> de ASP.NET Core Identity
/// (PBKDF2), sin usar el resto de Identity (research §5).
/// Su responsabilidad es calcular y comprobar hashes.
/// No valida la longitud de la contraseña, no la normaliza y no la guarda.
/// </summary>
public class HashContrasena : IHashContrasena
{
    private static readonly object SinUsuario = new();
    private readonly PasswordHasher<object> _calculador = new();

    /// <inheritdoc />
    public string Calcular(string contrasena) => _calculador.HashPassword(SinUsuario, contrasena);

    /// <inheritdoc />
    public bool Verificar(string hash, string contrasena)
    {
        try
        {
            return _calculador.VerifyHashedPassword(SinUsuario, hash, contrasena)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
