using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Base;

/// <summary>
/// Escribe directamente en la base lo que necesitan las pruebas de la spec 012: avisos de
/// actividad con sus destinatarios como los dejaría el servicio, su lectura y los conteos, para
/// preparar escenarios sin pasar por la API.
/// </summary>
public class SembradorAvisos
{
    private readonly FabricaApi _fabrica;

    public SembradorAvisos(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>
    /// Genera un aviso como lo haría el servicio: una fila de destinatario por cada jugador. Con
    /// <paramref name="cuando"/> el identificador v7 nace en ese instante, para ordenar avisos en
    /// una prueba sin esperar.
    /// </summary>
    public Task<AvisoDeActividad> GenerarAsync(
        Club club,
        HechoDeActividad hecho,
        TipoDeActividad actividadTipo,
        Guid actividadId,
        string texto,
        IReadOnlyCollection<UsuarioRol> destinatarios,
        DateTimeOffset? cuando = null) => _fabrica.ConContextoAsync(async contexto =>
    {
        var aviso = new AvisoDeActividad
        {
            Id = cuando is null ? Guid.CreateVersion7() : Guid.CreateVersion7(cuando.Value),
            ClubId = club.Id,
            Hecho = hecho,
            ActividadTipo = actividadTipo,
            ActividadId = actividadId,
            Texto = texto,
            GeneradoEn = cuando?.UtcDateTime ?? DateTime.UtcNow,
        };

        foreach (var jugador in destinatarios)
        {
            aviso.Destinatarios.Add(new AvisoDeActividadDestinatario
            {
                ClubId = club.Id, UsuarioRolId = jugador.Id, UsuarioId = jugador.UsuarioId,
            });
        }

        contexto.AvisosDeActividad.Add(aviso);
        await contexto.SaveChangesAsync();
        return aviso;
    });

    /// <summary>Marca el aviso como leído en todas las filas de esa cuenta.</summary>
    public Task<int> LeerAsync(AvisoDeActividad aviso, Usuario cuenta) => _fabrica.ConContextoAsync(contexto =>
        contexto.AvisosDeActividadDestinatario.IgnoreQueryFilters()
            .Where(fila => fila.AvisoId == aviso.Id && fila.UsuarioId == cuenta.Id && fila.LeidoEn == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.LeidoEn, DateTime.UtcNow)));

    /// <summary>Filas de destinatario de un aviso de esa cuenta, sin el filtro de club.</summary>
    public Task<List<AvisoDeActividadDestinatario>> LeerAsync(Guid avisoId, Usuario cuenta) =>
        _fabrica.ConContextoAsync(contexto => contexto.AvisosDeActividadDestinatario.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(fila => fila.AvisoId == avisoId && fila.UsuarioId == cuenta.Id)
            .ToListAsync());

    /// <summary>Cuántos avisos distintos tiene la cuenta sin leer, en todos los clubes.</summary>
    public Task<int> ContarAsync(Usuario cuenta) => _fabrica.ConContextoAsync(contexto =>
        contexto.AvisosDeActividadDestinatario.IgnoreQueryFilters()
            .Where(fila => fila.UsuarioId == cuenta.Id && fila.LeidoEn == null)
            .Select(fila => fila.AvisoId)
            .Distinct()
            .CountAsync());

    /// <summary>Todos los avisos de una actividad, sin el filtro de club, del más reciente al más antiguo.</summary>
    public Task<List<AvisoDeActividad>> DeLaActividadAsync(TipoDeActividad tipo, Guid actividadId) =>
        _fabrica.ConContextoAsync(contexto => contexto.AvisosDeActividad.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(aviso => aviso.Destinatarios)
            .Where(aviso => aviso.ActividadTipo == tipo && aviso.ActividadId == actividadId)
            .OrderByDescending(aviso => aviso.GeneradoEn)
            .ToListAsync());

    /// <summary>Cuántos avisos hay en total en la base, de cualquier club.</summary>
    public Task<int> ContarTodosAsync() => _fabrica.ConContextoAsync(contexto =>
        contexto.AvisosDeActividad.IgnoreQueryFilters().CountAsync());
}
