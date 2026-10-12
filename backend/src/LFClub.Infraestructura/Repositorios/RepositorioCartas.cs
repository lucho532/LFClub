using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las cartas de los jugadores del club de la petición con Entity Framework
/// (spec 010).
/// Su responsabilidad es leer una carta por su clave primaria y crearla o guardarla. Trabaja siempre
/// con el filtro de aislamiento activo.
/// No decide quién la cambia ni valida los valores: eso lo hacen el servicio y las reglas del dominio.
/// </summary>
public class RepositorioCartas : IRepositorioCartas
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioCartas(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<CartaJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.CartasJugador.AsNoTracking().FirstOrDefaultAsync(carta => carta.UsuarioRolId == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public async Task GuardarAsync(CartaJugador carta, bool esNueva, CancellationToken cancelacion = default)
    {
        if (esNueva)
        {
            _contexto.CartasJugador.Add(carta);
        }
        else
        {
            _contexto.CartasJugador.Update(carta);
        }

        await _contexto.SaveChangesAsync(cancelacion);
        _contexto.Entry(carta).State = EntityState.Detached;
    }
}
