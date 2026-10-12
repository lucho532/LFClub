using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los avisos de actividades del club de la petición con Entity Framework
/// (spec 012).
/// Su responsabilidad es resolver los destinatarios en una sola consulta y guardar los avisos con sus
/// destinatarios en la unidad de trabajo del llamador, reemplazando lo sin leer de la misma
/// actividad; la vista de la cuenta de un jugador (lista, contador, lectura y disponibilidad) está en
/// <c>RepositorioAvisos.Campana.cs</c>.
/// Trabaja siempre con el filtro de aislamiento activo.
/// No decide a quién llega cada hecho, no abre transacciones propias y no envía avisos fuera de la
/// aplicación.
/// </summary>
public partial class RepositorioAvisos : IRepositorioAvisos
{
    private readonly ContextoLaPecosa _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioAvisos(ContextoLaPecosa contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DestinatarioDeAviso>> ResolverDestinatariosAsync(
        CoberturaDeAviso cobertura,
        IReadOnlyCollection<Guid> categoriaIds,
        Guid? partidoId,
        IReadOnlyCollection<Guid> jugadorIds,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default)
    {
        var jugadores = JugadoresActivosDelClub().Where(integrante => integrante.UsuarioId != cuentaQueActua);
        var categorias = categoriaIds.ToList();
        var ids = jugadorIds.ToList();

        jugadores = cobertura switch
        {
            CoberturaDeAviso.CATEGORIAS => jugadores.Where(
                integrante => integrante.CategoriaId != null && categorias.Contains(integrante.CategoriaId.Value)),
            CoberturaDeAviso.CONVOCADOS => jugadores.Where(integrante => _contexto.PartidosJugador.Any(
                fila => fila.PartidoId == partidoId && fila.UsuarioRolId == integrante.Id)),
            _ => jugadores.Where(integrante => ids.Contains(integrante.Id)),
        };

        return await jugadores
            .Select(integrante => new DestinatarioDeAviso(integrante.Id, integrante.UsuarioId))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JugadorDeAviso>> JugadoresAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default)
    {
        var ids = usuarioRolIds.ToList();
        return await JugadoresActivosDelClub()
            .Where(integrante => ids.Contains(integrante.Id))
            .Select(integrante => new JugadorDeAviso(integrante.Id, integrante.UsuarioId, integrante.Nombres))
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task GenerarAsync(
        AvisoDeActividad aviso, IReadOnlyCollection<DestinatarioDeAviso> destinatarios, CancellationToken cancelacion = default)
    {
        if (destinatarios.Count == 0)
        {
            return;
        }

        // RF-025: antes de sumar el nuevo, cada cuenta destinataria suelta sus filas sin leer de la
        // misma actividad; el leído no se toca. Es un borrado del sistema (§14, enmienda 4.10.0).
        var cuentas = destinatarios.Select(fila => fila.UsuarioId).Distinct().ToList();
        await _contexto.AvisosDeActividadDestinatario
            .Where(fila => fila.LeidoEn == null
                && cuentas.Contains(fila.UsuarioId)
                && fila.Aviso!.ActividadTipo == aviso.ActividadTipo
                && fila.Aviso.ActividadId == aviso.ActividadId)
            .ExecuteDeleteAsync(cancelacion);
        await _contexto.AvisosDeActividad
            .Where(otro => otro.ActividadTipo == aviso.ActividadTipo
                && otro.ActividadId == aviso.ActividadId
                && !otro.Destinatarios.Any())
            .ExecuteDeleteAsync(cancelacion);

        foreach (var destinatario in destinatarios)
        {
            aviso.Destinatarios.Add(new AvisoDeActividadDestinatario
            {
                ClubId = aviso.ClubId,
                UsuarioRolId = destinatario.UsuarioRolId,
                UsuarioId = destinatario.UsuarioId,
            });
        }

        _contexto.AvisosDeActividad.Add(aviso);
        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        finally
        {
            foreach (var fila in aviso.Destinatarios.ToList())
            {
                _contexto.Entry(fila).State = EntityState.Detached;
            }

            _contexto.Entry(aviso).State = EntityState.Detached;
        }
    }

    /// <summary>Los jugadores activos del club: aprobados, sin retirar y con categoría.</summary>
    private IQueryable<UsuarioRol> JugadoresActivosDelClub() =>
        _contexto.UsuariosRol.AsNoTracking().Where(integrante =>
            integrante.Rol == Rol.JUGADOR
            && integrante.EstadoIngreso == EstadoIngreso.APROBADO
            && integrante.Activo
            && integrante.RetiradoEn == null
            && integrante.CategoriaId != null);
}
