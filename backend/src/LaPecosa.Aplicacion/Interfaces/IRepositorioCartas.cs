using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las cartas de los jugadores del club de la petición (spec 010).
/// Su responsabilidad es leer la carta de un jugador por su identificador y guardarla, creándola
/// con el primer cambio.
/// No decide quién la ve ni quién la cambia, no valida los valores y no recibe un identificador de
/// club.
/// </summary>
public interface IRepositorioCartas
{
    /// <summary>La carta del jugador, o nula si todavía no tiene (sin dorsal, posición ni números).</summary>
    Task<CartaJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Crea la carta si no existía o guarda sus cambios.</summary>
    Task GuardarAsync(CartaJugador carta, bool esNueva, CancellationToken cancelacion = default);
}
