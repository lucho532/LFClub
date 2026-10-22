using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador con saldo pendiente en las finanzas (spec 015, RF-033).
/// Su responsabilidad es llevar quién es, cuánto debe, su estado de cuenta y si está retirado.
/// No lleva sus cargos ni sus pagos: se abren desde su cuenta.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="NombreCompleto">Nombres y apellidos.</param>
/// <param name="Saldo">Lo que debe.</param>
/// <param name="Estado">Pago parcial o debe.</param>
/// <param name="Retirado">Si ya no está activo en el club.</param>
public record JugadorQueDebeDto(
    Guid UsuarioRolId, string NombreCompleto, decimal Saldo, EstadoDeCuenta Estado, bool Retirado);
