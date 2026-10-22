using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa un club en la lista del panel de administración.
/// Su responsabilidad es mostrar su nombre, su estado, su identidad y si su presidente ya se registró.
/// Incluye cuántos integrantes activos tiene por rol, para la facturación; no contiene datos personales
/// de ningún integrante ni información interna del club.
/// </summary>
/// <param name="ClubId">Identificador del club.</param>
/// <param name="Nombre">Nombre del club.</param>
/// <param name="Estado">Estado actual.</param>
/// <param name="PresidenteRegistrado">Verdadero si el club tiene algún integrante con rol PRESIDENTE.</param>
/// <param name="Identidad">Identidad visual del club.</param>
/// <param name="Integrantes">Integrantes activos del club contados por rol.</param>
/// <param name="EstadoDePago">Situación de pago frente a la plataforma (spec 017).</param>
/// <param name="ValorAdeudado">Suma de los cobros pendientes, en pesos (spec 017).</param>
/// <param name="MesDeInicioDelCobro">Mes <c>AAAA-MM</c> desde el que se cobra; nulo si no se cobra (spec 017).</param>
/// <param name="MotivoDeSuspension">Motivo de la suspensión; nulo si el club no está suspendido (spec 017).</param>
public record ClubResumenDto(
    Guid ClubId,
    string Nombre,
    EstadoClub Estado,
    bool PresidenteRegistrado,
    IdentidadClubDto Identidad,
    ConteoIntegrantesDto Integrantes,
    EstadoDePagoClub EstadoDePago = EstadoDePagoClub.SIN_COBRO,
    decimal ValorAdeudado = 0,
    string? MesDeInicioDelCobro = null,
    MotivoSuspension? MotivoDeSuspension = null);
