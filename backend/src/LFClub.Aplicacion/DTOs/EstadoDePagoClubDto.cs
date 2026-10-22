using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la situación de pago de un club frente a la plataforma (spec 017, RF-021).
/// Su responsabilidad es llevar el estado de pago, lo adeudado, el mes de inicio del cobro y el
/// motivo de la suspensión, para el panel del DESARROLLADOR.
/// No lleva datos personales ni llaves.
/// </summary>
/// <param name="EstadoDePago">Estado de pago calculado.</param>
/// <param name="ValorAdeudado">Suma de los cobros pendientes, en pesos.</param>
/// <param name="MesDeInicioDelCobro">Mes <c>AAAA-MM</c> desde el que se cobra; nulo si no se cobra.</param>
/// <param name="MotivoDeSuspension">Motivo de la suspensión; nulo si el club no está suspendido.</param>
public record EstadoDePagoClubDto(
    EstadoDePagoClub EstadoDePago,
    decimal ValorAdeudado,
    string? MesDeInicioDelCobro,
    MotivoSuspension? MotivoDeSuspension);
