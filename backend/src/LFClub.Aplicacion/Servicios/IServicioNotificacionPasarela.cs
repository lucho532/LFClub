using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la recepción de las notificaciones de la pasarela de pago (spec 017, RF-010 a RF-015).
/// Su responsabilidad es verificar la firma, asociar la notificación con el intento y el cobro,
/// comprobar el valor y, solo si todo cuadra y el pago fue aprobado, crear el pago, marcar el cobro como
/// pagado y reevaluar la suspensión, todo en una transacción. Registra cada notificación con su
/// resultado. Es idempotente: repetir una notificación no cambia nada.
/// Es la única vía, junto con la marca manual del DESARROLLADOR, por la que un cobro queda pagado: el
/// retorno del navegador no prueba nada (§7.4).
/// </summary>
public interface IServicioNotificacionPasarela
{
    /// <summary>
    /// Procesa una notificación. Con firma inválida responde <c>401 firma_invalida</c> después de
    /// registrarla; con firma válida devuelve qué pasó, aunque no cambie nada.
    /// </summary>
    Task<ResultadoNotificacion> RecibirAsync(
        string cuerpo, string? checksumDeLaCabecera, CancellationToken cancelacion = default);
}
