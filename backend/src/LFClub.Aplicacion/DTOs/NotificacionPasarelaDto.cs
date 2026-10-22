using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa una notificación recibida de la pasarela, para que el DESARROLLADOR revise duplicados
/// y casos raros (spec 017, RF-015).
/// Su responsabilidad es llevar lo registrado y lo que se hizo con ella.
/// No lleva el cuerpo de la notificación ni su firma.
/// </summary>
/// <param name="NotificacionId">Identificador de la notificación.</param>
/// <param name="RecibidaEn">Cuándo llegó, en UTC.</param>
/// <param name="Resultado">Qué pasó con ella.</param>
/// <param name="TransaccionId">Identificador de la transacción, si venía.</param>
/// <param name="EstadoDeLaPasarela">Estado informado por la pasarela.</param>
/// <param name="Referencia">Referencia del intento.</param>
/// <param name="Valor">Valor informado, en pesos.</param>
/// <param name="ClubId">Club del cobro asociado, si se pudo asociar.</param>
/// <param name="Periodo">Periodo <c>AAAA-MM</c> del cobro asociado.</param>
public record NotificacionPasarelaDto(
    Guid NotificacionId,
    DateTime RecibidaEn,
    ResultadoNotificacion Resultado,
    string? TransaccionId,
    string? EstadoDeLaPasarela,
    string? Referencia,
    decimal? Valor,
    Guid? ClubId,
    string? Periodo);
