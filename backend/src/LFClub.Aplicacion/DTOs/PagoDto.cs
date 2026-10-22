using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un pago, su recibo y quién hizo cada paso (spec 015, RF-018 a RF-025).
/// Su responsabilidad es llevar los datos del pago tal como los ven el PRESIDENTE, el DIRECTIVO y la
/// cuenta del jugador, con el nombre y el documento del jugador solo como nombre (el documento no
/// sale), y si tiene soporte y resguardo para que la pantalla ofrezca abrirlos.
/// No lleva los bytes del soporte ni del resguardo, ni el documento del jugador.
/// </summary>
/// <param name="Id">Identificador del pago.</param>
/// <param name="UsuarioRolId">Jugador; nulo si su cuenta se eliminó.</param>
/// <param name="JugadorNombre">Nombre del jugador copiado al crear el pago.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="Valor">Lo que cubre el pago.</param>
/// <param name="ValorInformado">Lo que informó la familia, solo si el PRESIDENTE lo corrigió.</param>
/// <param name="Medio">Efectivo o transferencia.</param>
/// <param name="Entidad">Entidad de la transferencia.</param>
/// <param name="Referencia">Número de comprobante.</param>
/// <param name="Fecha">Fecha en que se pagó.</param>
/// <param name="Concepto">Lo que cubre, en texto.</param>
/// <param name="NumeroRecibo">Consecutivo del club; nulo en revisión o rechazado.</param>
/// <param name="SaldoTrasPago">Lo que quedaba debiendo al confirmarse.</param>
/// <param name="TieneSoporte">Si la familia subió una imagen de soporte.</param>
/// <param name="TieneResguardo">Si hay resguardo para abrir: confirmado o anulado con jugador.</param>
/// <param name="CreadoEn">Cuándo se registró o informó.</param>
/// <param name="CreadoPorNombre">Quién lo registró o informó.</param>
/// <param name="ConfirmadoEn">Cuándo se confirmó.</param>
/// <param name="ConfirmadoPorNombre">Quién lo confirmó.</param>
/// <param name="RechazadoEn">Cuándo se rechazó.</param>
/// <param name="RechazadoPorNombre">Quién lo rechazó.</param>
/// <param name="MotivoRechazo">Por qué se rechazó.</param>
/// <param name="AnuladoEn">Cuándo se anuló.</param>
/// <param name="AnuladoPorNombre">Quién lo anuló.</param>
/// <param name="MotivoAnulacion">Por qué se anuló.</param>
public record PagoDto(
    Guid Id,
    Guid? UsuarioRolId,
    string JugadorNombre,
    EstadoPago Estado,
    decimal Valor,
    decimal? ValorInformado,
    MedioPago Medio,
    string? Entidad,
    string? Referencia,
    DateOnly Fecha,
    string? Concepto,
    int? NumeroRecibo,
    decimal? SaldoTrasPago,
    bool TieneSoporte,
    bool TieneResguardo,
    DateTime CreadoEn,
    string CreadoPorNombre,
    DateTime? ConfirmadoEn,
    string? ConfirmadoPorNombre,
    DateTime? RechazadoEn,
    string? RechazadoPorNombre,
    string? MotivoRechazo,
    DateTime? AnuladoEn,
    string? AnuladoPorNombre,
    string? MotivoAnulacion);
