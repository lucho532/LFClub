namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa el medio con el que se pagó (spec 015, RF-017).
/// Su responsabilidad es distinguir el dinero recibido en mano del recibido por transferencia a la
/// cuenta del club; la aplicación no mueve dinero en ninguno de los dos (§6.2).
/// No guarda la entidad de la transferencia: eso es un texto del pago.
/// </summary>
public enum MedioPago
{
    /// <summary>Dinero entregado en mano al PRESIDENTE.</summary>
    EFECTIVO,

    /// <summary>Transferencia a una cuenta del club; exige indicar la entidad.</summary>
    TRANSFERENCIA,
}
