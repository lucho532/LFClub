using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de registrar un pago recibido directamente por el PRESIDENTE (spec 015,
/// RF-020 y RF-023 a RF-025).
/// Su responsabilidad es dejar el pago confirmado y aplicado a los cargos del jugador, con su recibo
/// numerado, de modo que su estado de cuenta cambie en el acto.
/// No comprueba el rol (lo hace la autorización del controlador) ni mueve dinero: el club recibe el
/// dinero fuera de la aplicación (§6.2).
/// </summary>
public interface IServicioRegistroDePagos
{
    /// <summary>
    /// Registra y confirma un pago de ese jugador, también si está retirado y debe. Valor mayor que lo
    /// pendiente: 409 <c>pago_excede_deuda</c>; sin nada pendiente: 409 <c>sin_deuda</c>; datos
    /// inválidos: 400; jugador que no es del club: 404.
    /// </summary>
    Task<PagoDto> RegistrarAsync(
        Guid usuarioRolId, RegistrarPagoDto datos, UsuarioRol quienRegistra, CancellationToken cancelacion = default);
}
