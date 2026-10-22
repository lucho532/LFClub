using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el estado de cuenta de un jugador (spec 015, RF-001 a RF-004).
/// Su responsabilidad es llevar su estado, su saldo, sus cargos, sus pagos y los medios de pago
/// activos del club, y decir a quien la ve qué puede hacer: registrar un pago (solo el PRESIDENTE) o
/// informar una transferencia (solo la cuenta del propio jugador).
/// No lleva el documento del jugador ni condiciona ningún otro acceso: deber no restringe nada.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="NombreCompleto">Nombres y apellidos.</param>
/// <param name="Estado">Al día, pago parcial o debe.</param>
/// <param name="Saldo">Suma de lo pendiente de sus cargos.</param>
/// <param name="Cargos">Cargos, del más reciente al más antiguo.</param>
/// <param name="Pagos">Pagos de cualquier estado, del más reciente al más antiguo.</param>
/// <param name="MediosPago">Medios de pago activos del club.</param>
/// <param name="PuedeRegistrar">Verdadero solo para el PRESIDENTE.</param>
/// <param name="PuedeInformar">Verdadero solo para la cuenta del propio jugador.</param>
public record CuentaJugadorDto(
    Guid UsuarioRolId,
    string NombreCompleto,
    EstadoDeCuenta Estado,
    decimal Saldo,
    IReadOnlyList<CargoDto> Cargos,
    IReadOnlyList<PagoDto> Pagos,
    IReadOnlyList<MedioPagoDto> MediosPago,
    bool PuedeRegistrar,
    bool PuedeInformar);
