using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso con los que se retira del club a un jugador que se fue, por su
/// PRESIDENTE o por su propia familia, y con los que el PRESIDENTE lo reincorpora si vuelve
/// (constitución §14.1; RF-041 a RF-047 de la 003 y RF-041 a RF-043 de la 006).
/// Su responsabilidad es dejar al jugador retirado, fuera de su categoría y de sus equipos y sin
/// acceso al club, conservando todos sus datos salvo su foto propia, que se borra (§14.2, spec 007),
/// y devolverlo al club, sin foto, ubicándolo como a un ingreso recién aprobado, siempre que su
/// documento no esté activo con otra cuenta en otro club.
/// No borra nada más, no retira a quien no es un jugador aprobado y no envía correos. El rol mínimo de
/// cada operación lo comprueba la autorización; a quién puede retirar cada uno, el servicio. No
/// hace el retiro automático al aprobarse un hermano en otro club, que es de la aprobación.
/// </summary>
public interface IServicioRetiroJugador
{
    /// <summary>
    /// Retira al jugador. <paramref name="quienRetira"/> es el integrante que hace la petición: el
    /// PRESIDENTE retira a cualquiera; la cuenta de un jugador, solo a ese jugador, y a cualquier
    /// otro le responde <c>404 no_encontrado</c>. Retirar a quien ya está retirado no tiene efecto.
    /// Su foto se borra en la misma operación.
    /// </summary>
    Task RetirarAsync(Guid usuarioRolId, UsuarioRol quienRetira, CancellationToken cancelacion = default);

    /// <summary>
    /// Reincorpora a un jugador retirado y lo ubica en la categoría activa de su año de nacimiento,
    /// si el club la tiene. No recupera sus equipos. Si su documento está aprobado y activo en
    /// otra cuenta, en cualquier club, <c>409 documento_activo_en_otro_club</c> y nada cambia.
    /// </summary>
    Task<ReincorporacionDto> ReincorporarAsync(Guid usuarioRolId, CancellationToken cancelacion = default);
}
