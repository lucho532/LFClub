namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el tipo de documento de identidad de una persona (constitución §10).
/// Su responsabilidad es nombrar los tipos admitidos.
/// No valida el formato del número de documento.
/// </summary>
public enum TipoDocumento
{
    /// <summary>Registro civil.</summary>
    REGISTRO_CIVIL,

    /// <summary>Tarjeta de identidad.</summary>
    TARJETA_IDENTIDAD,

    /// <summary>Cédula de ciudadanía.</summary>
    CEDULA_CIUDADANIA,

    /// <summary>Cédula de extranjería.</summary>
    CEDULA_EXTRANJERIA,
}
