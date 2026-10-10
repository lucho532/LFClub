using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las fotos de los jugadores con Entity Framework.
/// Su responsabilidad es leer una foto completa solo cuando se va a servir, consultar las fechas
/// con proyecciones que no leen la columna del contenido, guardar una foto sustituyendo a la
/// anterior y borrarla con una sola sentencia. Trabaja siempre con el filtro de aislamiento activo.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioFotosJugador : IRepositorioFotosJugador
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioFotosJugador(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<FotoJugador?> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.FotosJugador.AsNoTracking().FirstOrDefaultAsync(foto => foto.UsuarioRolId == usuarioRolId, cancelacion);

    /// <inheritdoc />
    public Task<DateTime?> FechaAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.FotosJugador
            .Where(foto => foto.UsuarioRolId == usuarioRolId)
            .Select(foto => (DateTime?)foto.GuardadaEn)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, DateTime>> FechasAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default)
    {
        if (usuarioRolIds.Count == 0)
        {
            return new Dictionary<Guid, DateTime>();
        }

        return await _contexto.FotosJugador
            .Where(foto => usuarioRolIds.Contains(foto.UsuarioRolId))
            .Select(foto => new { foto.UsuarioRolId, foto.GuardadaEn })
            .ToDictionaryAsync(fila => fila.UsuarioRolId, fila => fila.GuardadaEn, cancelacion);
    }

    /// <inheritdoc />
    public async Task GuardarAsync(
        Guid usuarioRolId,
        Guid clubId,
        byte[] contenido,
        string tipoContenido,
        DateTime guardadaEn,
        CancellationToken cancelacion = default)
    {
        var foto = await _contexto.FotosJugador.FirstOrDefaultAsync(foto => foto.UsuarioRolId == usuarioRolId, cancelacion);
        if (foto is null)
        {
            _contexto.FotosJugador.Add(new FotoJugador
            {
                UsuarioRolId = usuarioRolId,
                ClubId = clubId,
                Contenido = contenido,
                TipoContenido = tipoContenido,
                GuardadaEn = guardadaEn,
            });
            return;
        }

        foto.Contenido = contenido;
        foto.TipoContenido = tipoContenido;
        foto.GuardadaEn = guardadaEn;
    }

    /// <inheritdoc />
    public Task BorrarAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.FotosJugador.Where(foto => foto.UsuarioRolId == usuarioRolId).ExecuteDeleteAsync(cancelacion);
}
