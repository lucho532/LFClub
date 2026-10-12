using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso al club de la petición, para sus propios integrantes.
/// Su responsabilidad es leer el club que la autorización fijó en <see cref="IContextoClub"/> y
/// bloquear su fila para que las operaciones que ubican jugadores se ejecuten una detrás de otra
/// dentro de un mismo club (research §4 de la 003).
/// No recibe un identificador de club ni permite leer o bloquear ningún otro: sin club en el
/// contexto no devuelve nada.
/// </summary>
public interface IRepositorioClub
{
    /// <summary>
    /// Bloquea la fila del club de la petición hasta que termine la transacción en curso. Debe
    /// llamarse dentro de una transacción y antes de leer lo que se va a cambiar. Devuelve el
    /// identificador de ese club.
    /// </summary>
    Task<Guid> BloquearAsync(CancellationToken cancelacion = default);

    /// <summary>El club de la petición, o nulo si no hay ninguno en el contexto.</summary>
    Task<Club?> ObtenerAsync(CancellationToken cancelacion = default);
}
