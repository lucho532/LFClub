using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

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
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de datos.</summary>
    public RepositorioAvisos(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DestinatarioDeAviso>> ResolverDestinatariosAsync(
        CoberturaDeAviso cobertura,
        IReadOnlyCollection<Guid> categoriaIds,
        Guid? actividadId,
        IReadOnlyCollection<Guid> jugadorIds,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default)
    {
        var jugadores = JugadoresActivosDelClub().Where(integrante => integrante.UsuarioId != cuentaQueActua);
        var categorias = categoriaIds.ToList();
        var ids = jugadorIds.ToList();

        jugadores = cobertura switch
        {
            CoberturaDeAviso.CATEGORIAS => jugadores.Where(integrante =>
                (integrante.CategoriaId != null && categorias.Contains(integrante.CategoriaId.Value))
                || _contexto.PartidosJugador.Any(fila => fila.PartidoId == actividadId && fila.UsuarioRolId == integrante.Id)),
            CoberturaDeAviso.CONVOCADOS => jugadores.Where(integrante => _contexto.PartidosJugador.Any(
                fila => fila.PartidoId == actividadId && fila.UsuarioRolId == integrante.Id)),
            CoberturaDeAviso.AUDIENCIA_DE_ENTRENAMIENTO => jugadores.Where(integrante =>
                (categorias.Count == 0 || categorias.Contains(integrante.CategoriaId!.Value))
                && (_contexto.EntrenamientosJugador.Any(
                        fila => fila.EntrenamientoId == actividadId && fila.UsuarioRolId == integrante.Id)
                    || (_contexto.EntrenamientosEquipo.Any(fila => fila.EntrenamientoId == actividadId)
                        ? _contexto.JugadoresEquipo.Any(pertenencia => pertenencia.UsuarioRolId == integrante.Id
                            && _contexto.EntrenamientosEquipo.Any(fila => fila.EntrenamientoId == actividadId
                                && fila.EquipoId == pertenencia.EquipoId))
                        : _contexto.EntrenamientosCategoria.Any(fila => fila.EntrenamientoId == actividadId
                            && fila.CategoriaId == integrante.CategoriaId)))),
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

    /// <inheritdoc />
    public async Task BorrarSinLeerAsync(
        TipoDeActividad tipo,
        Guid actividadId,
        IReadOnlyCollection<Guid> usuarioRolIds,
        CancellationToken cancelacion = default)
    {
        if (usuarioRolIds.Count == 0)
        {
            return;
        }

        var ids = usuarioRolIds.ToList();
        await _contexto.AvisosDeActividadDestinatario
            .Where(fila => fila.LeidoEn == null
                && ids.Contains(fila.UsuarioRolId)
                && fila.Aviso!.ActividadTipo == tipo
                && fila.Aviso.ActividadId == actividadId)
            .ExecuteDeleteAsync(cancelacion);
        await _contexto.AvisosDeActividad
            .Where(aviso => aviso.ActividadTipo == tipo && aviso.ActividadId == actividadId && !aviso.Destinatarios.Any())
            .ExecuteDeleteAsync(cancelacion);
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
