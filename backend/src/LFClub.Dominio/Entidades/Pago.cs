using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa dinero recibido de un jugador (spec 015, RF-017 a RF-023; constitución §16.3).
/// Su responsabilidad es guardar el valor, el medio, la entidad, la fecha, el estado y, de cada paso
/// (registro, confirmación, rechazo, anulación), quién lo dio y cuándo, con el nombre copiado (§13).
/// También copia el nombre y el documento del jugador y el concepto, porque el pago se conserva sin
/// jugador cuando su cuenta se elimina (spec 014). Un pago confirmado no se edita ni se borra: solo
/// se anula. Un pago <c>EN_REVISION</c> no tiene aplicaciones, recibo ni resguardo.
/// No guarda el saldo del jugador (solo <see cref="SaldoTrasPago"/> al confirmarse) ni la imagen del
/// resguardo, que se dibuja a demanda.
/// </summary>
public class Pago : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Jugador que pagó; pasa a nulo si su cuenta se elimina. Foránea con borrado a nulo.</summary>
    public Guid? UsuarioRolId { get; set; }

    /// <summary>Nombres y apellidos del jugador, copiados al crear el pago. Máx. 200.</summary>
    public string JugadorNombre { get; set; } = string.Empty;

    /// <summary>Tipo de documento del jugador, copiado al crear el pago. Máx. 30.</summary>
    public string JugadorTipoDocumento { get; set; } = string.Empty;

    /// <summary>Número de documento del jugador, copiado al crear el pago. Máx. 30.</summary>
    public string JugadorNumeroDocumento { get; set; } = string.Empty;

    /// <summary>Medio: efectivo o transferencia.</summary>
    public MedioPago Medio { get; set; }

    /// <summary>Entidad de la transferencia. Máx. 40; obligatoria en <c>TRANSFERENCIA</c>, nula en <c>EFECTIVO</c>.</summary>
    public string? Entidad { get; set; }

    /// <summary>Número de comprobante. Máx. 60. Opcional.</summary>
    public string? Referencia { get; set; }

    /// <summary>Fecha en que se pagó. No futura.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Lo que cubre el pago (el confirmado), en pesos enteros. Mayor que cero.</summary>
    public decimal Valor { get; set; }

    /// <summary>Lo que informó la familia, solo si el PRESIDENTE lo corrigió al confirmar (RF-022).</summary>
    public decimal? ValorInformado { get; set; }

    /// <summary>Estado actual.</summary>
    public EstadoPago Estado { get; set; }

    /// <summary>Texto de lo que cubre ("Mensualidad octubre 2026"), copiado al confirmar. Máx. 300.</summary>
    public string? Concepto { get; set; }

    /// <summary>Consecutivo del club; solo con <c>CONFIRMADO</c> o <c>ANULADO</c>.</summary>
    public int? NumeroRecibo { get; set; }

    /// <summary>Lo que quedaba debiendo el jugador al confirmarse; 0 es "al día".</summary>
    public decimal? SaldoTrasPago { get; set; }

    /// <summary>Cuándo se registró o informó, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>Quién lo registró o informó; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? CreadoPorUsuarioId { get; set; }

    /// <summary>Nombre de quien lo registró o informó, copiado. Máx. 200.</summary>
    public string CreadoPorNombre { get; set; } = string.Empty;

    /// <summary>Cuándo se confirmó, en UTC.</summary>
    public DateTime? ConfirmadoEn { get; set; }

    /// <summary>Quién lo confirmó; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? ConfirmadoPorUsuarioId { get; set; }

    /// <summary>Nombre de quien lo confirmó, copiado. Máx. 200.</summary>
    public string? ConfirmadoPorNombre { get; set; }

    /// <summary>Cuándo se rechazó, en UTC.</summary>
    public DateTime? RechazadoEn { get; set; }

    /// <summary>Quién lo rechazó; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? RechazadoPorUsuarioId { get; set; }

    /// <summary>Nombre de quien lo rechazó, copiado. Máx. 200.</summary>
    public string? RechazadoPorNombre { get; set; }

    /// <summary>Motivo del rechazo. Máx. 300; no vacío cuando hay rechazo.</summary>
    public string? MotivoRechazo { get; set; }

    /// <summary>Cuándo se anuló, en UTC.</summary>
    public DateTime? AnuladoEn { get; set; }

    /// <summary>Quién lo anuló; pasa a nulo si esa cuenta se elimina.</summary>
    public Guid? AnuladoPorUsuarioId { get; set; }

    /// <summary>Nombre de quien lo anuló, copiado. Máx. 200.</summary>
    public string? AnuladoPorNombre { get; set; }

    /// <summary>Motivo de la anulación. Máx. 300; no vacío cuando hay anulación.</summary>
    public string? MotivoAnulacion { get; set; }

    /// <summary>Cargos que cubre; vacío mientras está en revisión.</summary>
    public List<AplicacionPago> Aplicaciones { get; set; } = [];
}
