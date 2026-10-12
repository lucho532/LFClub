namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el cálculo y la comprobación del hash de las contraseñas (constitución §12.4).
/// Su responsabilidad es que ninguna contraseña se guarde en texto plano.
/// No valida la longitud de la contraseña ni la normaliza.
/// </summary>
public interface IHashContrasena
{
    /// <summary>Calcula el hash de una contraseña.</summary>
    string Calcular(string contrasena);

    /// <summary>Indica si la contraseña corresponde al hash guardado.</summary>
    bool Verificar(string hash, string contrasena);
}
