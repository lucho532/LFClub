using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que ve el DESARROLLADOR de un club.
/// Su responsabilidad es mostrar su configuración, su identidad, su estado, sus presidentes y sus invitaciones sin usar.
/// No incluye fichas, datos médicos, finanzas ni información deportiva (constitución §8).
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Sede">Sede.</param>
/// <param name="Direccion">Dirección.</param>
/// <param name="CorreoContacto">Correo de contacto.</param>
/// <param name="TelefonoContacto">Teléfono de contacto.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="EstadoCambiadoEn">Cuándo se hizo el último cambio de estado, o nulo.</param>
/// <param name="Identidad">Identidad visual del club.</param>
/// <param name="Presidentes">Presidentes registrados.</param>
/// <param name="Invitaciones">Invitaciones sin usar ni anular.</param>
/// <param name="EstadoDePago">Situación de pago frente a la plataforma (spec 017).</param>
/// <param name="ValorAdeudado">Suma de los cobros pendientes, en pesos (spec 017).</param>
/// <param name="MesDeInicioDelCobro">Mes <c>AAAA-MM</c> desde el que se cobra; nulo si no se cobra (spec 017).</param>
/// <param name="MotivoDeSuspension">Motivo de la suspensión; nulo si el club no está suspendido (spec 017).</param>
/// <param name="Integrantes">Integrantes activos del club contados por rol; solo números, sin datos personales.</param>
public record ClubDetalleDto(
    Guid ClubId,
    string Nombre,
    string? Sede,
    string? Direccion,
    string? CorreoContacto,
    string? TelefonoContacto,
    EstadoClub Estado,
    DateTime? EstadoCambiadoEn,
    IdentidadClubDto Identidad,
    IReadOnlyList<PresidenteDto> Presidentes,
    IReadOnlyList<InvitacionDto> Invitaciones,
    EstadoDePagoClub EstadoDePago = EstadoDePagoClub.SIN_COBRO,
    decimal ValorAdeudado = 0,
    string? MesDeInicioDelCobro = null,
    MotivoSuspension? MotivoDeSuspension = null,
    ConteoIntegrantesDto? Integrantes = null);
