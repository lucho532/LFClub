using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las fichas de los jugadores del club de la petición con Entity
/// Framework.
/// Su responsabilidad es leer la ficha de un jugador y sellar su último cambio, creando la fila
/// con el primero. Trabaja siempre con el filtro de aislamiento activo: sin club en el contexto no
/// devuelve nada.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioFichas : IRepositorioFichas
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioFichas(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<FichaJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.FichasJugador.AsNoTracking()
            .FirstOrDefaultAsync(ficha => ficha.UsuarioRolId == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public async Task<FichaJugador> SellarCambioAsync(
        UsuarioRol jugador, UsuarioRol quienCambia, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        var ficha = await _contexto.FichasJugador
            .FirstOrDefaultAsync(fila => fila.UsuarioRolId == jugador.Id, cancelacion);
        if (ficha is null)
        {
            ficha = new FichaJugador { UsuarioRolId = jugador.Id, ClubId = jugador.ClubId };
            _contexto.FichasJugador.Add(ficha);
        }

        ficha.UltimoCambioEn = ahoraUtc;
        ficha.UltimoCambioPorUsuarioId = quienCambia.UsuarioId;
        ficha.UltimoCambioPorNombre = $"{quienCambia.Nombres} {quienCambia.Apellidos}";
        return ficha;
    }
}
