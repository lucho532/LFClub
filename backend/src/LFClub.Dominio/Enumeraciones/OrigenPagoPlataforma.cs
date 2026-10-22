namespace LFClub.Dominio.Enumeraciones;

/// <summary>
/// Representa de dónde viene el pago de un cobro de la plataforma (spec 017).
/// Su responsabilidad es distinguir el pago confirmado por la pasarela del marcado a mano.
/// No guarda detalles del pago.
/// </summary>
public enum OrigenPagoPlataforma
{
    /// <summary>Confirmado por una notificación verificada de la pasarela.</summary>
    PASARELA,

    /// <summary>Marcado a mano por el DESARROLLADOR, por ejemplo un pago hecho por fuera.</summary>
    MANUAL,
}
