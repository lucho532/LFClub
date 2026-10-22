namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el estado de cuenta de un jugador (spec 015, RF-025).
/// Su responsabilidad es resumir su saldo en tres valores que ven el PRESIDENTE, el DIRECTIVO y su
/// familia.
/// No se guarda: lo calcula <c>ReglaEstadoDeCuenta</c> y no restringe ningún acceso (§16.8).
/// </summary>
public enum EstadoDeCuenta
{
    /// <summary>No debe nada.</summary>
    AL_DIA,

    /// <summary>Debe algo, pero ya se le aplicó parte de lo cobrado.</summary>
    PAGO_PARCIAL,

    /// <summary>Debe algo y no se le ha aplicado ningún pago.</summary>
    DEBE,
}
