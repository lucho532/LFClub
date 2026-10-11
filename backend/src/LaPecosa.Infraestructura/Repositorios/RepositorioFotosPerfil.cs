using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las fotos de perfil con Entity Framework.
/// Su responsabilidad es leer, guardar y quitar la foto de la cuenta indicada, y leer la de un
/// integrante del club de la petición con el filtro de aislamiento activo.
/// No contiene reglas de negocio ni consulta más de una cuenta a la vez.
/// </summary>
public class RepositorioFotosPerfil : IRepositorioFotosPerfil
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioFotosPerfil(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<FotoPerfil?> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion = default) =>
        _contexto.FotosPerfil.FirstOrDefaultAsync(foto => foto.UsuarioId == usuarioId, cancelacion);

    /// <inheritdoc />
    public Task<FotoPerfil?> ObtenerDeIntegranteAsync(Guid usuarioRolId, CancellationToken cancelacion = default) =>
        _contexto.UsuariosRol
            .Where(integrante => integrante.Id == usuarioRolId
                && integrante.Rol != Rol.JUGADOR
                && integrante.EstadoIngreso == EstadoIngreso.APROBADO)
            .Join(_contexto.FotosPerfil, integrante => integrante.UsuarioId, foto => foto.UsuarioId, (_, foto) => foto)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task GuardarAsync(
        Guid usuarioId, byte[] contenido, string tipoContenido, CancellationToken cancelacion = default)
    {
        var foto = await ObtenerAsync(usuarioId, cancelacion);
        if (foto is null)
        {
            _contexto.FotosPerfil.Add(new FotoPerfil { UsuarioId = usuarioId, Contenido = contenido, TipoContenido = tipoContenido });
            return;
        }

        foto.Contenido = contenido;
        foto.TipoContenido = tipoContenido;
    }

    /// <inheritdoc />
    public async Task QuitarAsync(Guid usuarioId, CancellationToken cancelacion = default)
    {
        var foto = await ObtenerAsync(usuarioId, cancelacion);
        if (foto is not null)
        {
            _contexto.FotosPerfil.Remove(foto);
        }
    }
}
