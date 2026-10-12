using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los archivos de la ficha con Entity Framework.
/// Su responsabilidad es consultar el estado de los documentos con proyecciones que no leen la
/// columna del contenido, leer un archivo completo solo cuando se va a abrir y guardar un archivo
/// sustituyendo al anterior. Trabaja siempre con el filtro de aislamiento activo.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioDocumentosJugador : IRepositorioDocumentosJugador
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioDocumentosJugador(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentoEntregado>> EstadoAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.DocumentosJugador
            .Where(archivo => archivo.UsuarioRolId == usuarioRolId)
            .Select(archivo => new DocumentoEntregado(
                archivo.Documento, archivo.SubidoEn, archivo.TipoContenido, archivo.TamanoBytes))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarEntregadosAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default)
    {
        if (usuarioRolIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _contexto.DocumentosJugador
            .Where(archivo => usuarioRolIds.Contains(archivo.UsuarioRolId))
            .GroupBy(archivo => archivo.UsuarioRolId)
            .Select(grupo => new { UsuarioRolId = grupo.Key, Entregados = grupo.Count() })
            .ToDictionaryAsync(fila => fila.UsuarioRolId, fila => fila.Entregados, cancelacion);
    }

    /// <inheritdoc />
    public Task<DocumentoJugador?> ObtenerAsync(
        Guid usuarioRolId, DocumentoPedido documento, CancellationToken cancelacion = default) =>
        _contexto.DocumentosJugador.AsNoTracking()
            .FirstOrDefaultAsync(
                archivo => archivo.UsuarioRolId == usuarioRolId && archivo.Documento == documento, cancelacion);

    /// <inheritdoc />
    public async Task GuardarAsync(
        UsuarioRol jugador,
        DocumentoPedido documento,
        byte[] contenido,
        string tipoContenido,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var sustituidos = await _contexto.DocumentosJugador
            .Where(archivo => archivo.UsuarioRolId == jugador.Id && archivo.Documento == documento)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(archivo => archivo.Contenido, contenido)
                    .SetProperty(archivo => archivo.TipoContenido, tipoContenido)
                    .SetProperty(archivo => archivo.TamanoBytes, contenido.Length)
                    .SetProperty(archivo => archivo.SubidoEn, ahoraUtc),
                cancelacion);
        if (sustituidos > 0)
        {
            return;
        }

        _contexto.DocumentosJugador.Add(new DocumentoJugador
        {
            UsuarioRolId = jugador.Id,
            Documento = documento,
            ClubId = jugador.ClubId,
            Contenido = contenido,
            TipoContenido = tipoContenido,
            TamanoBytes = contenido.Length,
            SubidoEn = ahoraUtc,
        });
        await _contexto.SaveChangesAsync(cancelacion);
    }
}
