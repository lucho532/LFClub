using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa la parte de <see cref="RepositorioComunicados"/> que ve la cuenta de un jugador (spec
/// 011, "Comunicados de una cuenta" del modelo de datos).
/// Su responsabilidad es traducir a SQL qué filas cuentan para la cuenta (jugador activo, comunicado
/// sin retirar y, si la sesión está limitada, solo esos jugadores), agrupar por comunicado para que
/// un comunicado que llega por dos hermanos salga una vez, ordenar por identificador descendente y
/// paginar con cursor, contar los sin leer y poner la lectura de la cuenta.
/// No decide quién es la cuenta ni qué rol puede pedirlo, y no borra nada.
/// </summary>
public partial class RepositorioComunicados
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RecibidoDeCuenta>> ListarRecibidosAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        Guid? antesDe,
        int limite,
        CancellationToken cancelacion = default)
    {
        var grupos = Agrupados(usuarioId, limitacion);
        if (antesDe is { } cursor)
        {
            grupos = grupos.Where(grupo => grupo.ComunicadoId.CompareTo(cursor) < 0);
        }

        var pagina = await grupos.OrderByDescending(grupo => grupo.ComunicadoId).Take(limite).ToListAsync(cancelacion);
        return await ConsultarAsync(pagina, cancelacion);
    }

    /// <inheritdoc />
    public Task<int> ContarSinLeerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default) =>
        Visibles(usuarioId, limitacion)
            .Where(fila => fila.LeidoEn == null)
            .Select(fila => fila.ComunicadoId)
            .Distinct()
            .CountAsync(cancelacion);

    /// <inheritdoc />
    public async Task<RecibidoDeCuenta?> ObtenerRecibidoAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, Guid comunicadoId, CancellationToken cancelacion = default)
    {
        var grupo = await Agrupados(usuarioId, limitacion)
            .Where(fila => fila.ComunicadoId == comunicadoId)
            .FirstOrDefaultAsync(cancelacion);
        return grupo is null ? null : (await ConsultarAsync([grupo], cancelacion)).SingleOrDefault();
    }

    /// <inheritdoc />
    public Task MarcarLeidoAsync(Guid usuarioId, Guid comunicadoId, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.ComunicadosDestinatario
            .Where(fila => fila.UsuarioId == usuarioId && fila.ComunicadoId == comunicadoId && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, ahoraUtc), cancelacion);

    /// <inheritdoc />
    public async Task MarcarTodosLeidosAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, DateTime ahoraUtc, CancellationToken cancelacion = default)
    {
        var pendientes = await Visibles(usuarioId, limitacion)
            .Where(fila => fila.LeidoEn == null)
            .Select(fila => fila.ComunicadoId)
            .Distinct()
            .ToListAsync(cancelacion);
        if (pendientes.Count == 0)
        {
            return;
        }

        await _contexto.ComunicadosDestinatario
            .Where(fila => fila.UsuarioId == usuarioId && pendientes.Contains(fila.ComunicadoId) && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, ahoraUtc), cancelacion);
    }

    /// <summary>
    /// Las filas que cuentan para la cuenta: de su cuenta, de un jugador activo, de un comunicado
    /// sin retirar y, si la sesión está limitada, de esos jugadores.
    /// </summary>
    private IQueryable<ComunicadoDestinatario> Visibles(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion)
    {
        var filas = _contexto.ComunicadosDestinatario.AsNoTracking().Where(fila =>
            fila.UsuarioId == usuarioId
            && fila.Comunicado!.RetiradoEn == null
            && fila.UsuarioRol!.Rol == Rol.JUGADOR
            && fila.UsuarioRol.EstadoIngreso == EstadoIngreso.APROBADO
            && fila.UsuarioRol.Activo);

        if (limitacion is null)
        {
            return filas;
        }

        var jugadores = limitacion.ToList();
        return filas.Where(fila => jugadores.Contains(fila.UsuarioRolId));
    }

    /// <summary>Una fila por comunicado: sin leer si alguna fila de la cuenta lo está.</summary>
    private IQueryable<GrupoRecibido> Agrupados(Guid usuarioId, IReadOnlyCollection<Guid>? limitacion) =>
        Visibles(usuarioId, limitacion)
            .GroupBy(fila => fila.ComunicadoId)
            .Select(grupo => new GrupoRecibido
            {
                ComunicadoId = grupo.Key,
                SinLeer = grupo.Any(fila => fila.LeidoEn == null),
                LeidoEn = grupo.Max(fila => fila.LeidoEn),
            });

    private async Task<IReadOnlyList<RecibidoDeCuenta>> ConsultarAsync(
        IReadOnlyList<GrupoRecibido> grupos, CancellationToken cancelacion)
    {
        var ids = grupos.Select(grupo => grupo.ComunicadoId).ToList();
        var comunicados = await _contexto.Comunicados.AsNoTracking()
            .Where(comunicado => ids.Contains(comunicado.Id))
            .ToDictionaryAsync(comunicado => comunicado.Id, cancelacion);

        return grupos
            .Where(grupo => comunicados.ContainsKey(grupo.ComunicadoId))
            .Select(grupo => new RecibidoDeCuenta(
                comunicados[grupo.ComunicadoId], !grupo.SinLeer, grupo.SinLeer ? null : grupo.LeidoEn))
            .ToList();
    }

    /// <summary>El resultado de agrupar las filas de una cuenta por comunicado.</summary>
    private sealed class GrupoRecibido
    {
        public Guid ComunicadoId { get; init; }

        public bool SinLeer { get; init; }

        public DateTime? LeidoEn { get; init; }
    }
}
