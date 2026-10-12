using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la ubicación automática de los jugadores en la categoría de su año de nacimiento
/// (constitución §12.1.1; RF-008 a RF-011), que comparten la aprobación de un ingreso, la
/// reincorporación y el alta y la reactivación de una categoría.
/// Su responsabilidad es poner a un jugador sin categoría en la categoría activa de su año, o
/// recoger en una categoría a todos los jugadores sin categoría nacidos ese año, y marcar la
/// categoría como usada cuando entra alguien.
/// No mueve nunca a quien ya tiene categoría (RF-011), no pone a nadie en un equipo (RF-026) y no
/// abre la transacción ni bloquea el club: quien lo llama debe tener ya el club bloqueado dentro
/// de su transacción (research §4).
/// </summary>
public class UbicadorDeJugadores
{
    private readonly IRepositorioCategorias _categorias;
    private readonly IRepositorioJugadores _jugadores;

    /// <summary>Crea el ubicador con sus dependencias.</summary>
    public UbicadorDeJugadores(IRepositorioCategorias categorias, IRepositorioJugadores jugadores)
    {
        _categorias = categorias;
        _jugadores = jugadores;
    }

    /// <summary>
    /// Pone al jugador en la categoría activa de su año de nacimiento, si el club la tiene.
    /// Devuelve esa categoría, o nulo si quedó sin categoría.
    /// </summary>
    public async Task<Categoria?> UbicarAUnoAsync(UsuarioRol jugador, CancellationToken cancelacion = default)
    {
        var categoria = await _categorias.BuscarPorAnioAsync(jugador.FechaNacimiento.Year, cancelacion);
        if (categoria is not { Activa: true }
            || !await _jugadores.UbicarSiNoTieneAsync(jugador.Id, categoria.Id, cancelacion))
        {
            return null;
        }

        await _categorias.MarcarUsadaAsync(categoria.Id, cancelacion);
        return categoria;
    }

    /// <summary>
    /// Recoge en la categoría a los jugadores sin categoría nacidos en su año. Devuelve cuántos
    /// entraron.
    /// </summary>
    public async Task<int> RecogerAsync(Categoria categoria, CancellationToken cancelacion = default)
    {
        var ubicados = await _jugadores.RecogerDelAnioAsync(categoria.Id, categoria.Anio, cancelacion);
        if (ubicados > 0)
        {
            await _categorias.MarcarUsadaAsync(categoria.Id, cancelacion);
        }

        return ubicados;
    }
}
