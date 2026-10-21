using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el borrado masivo de una cuenta y de todo lo suyo (spec 014, constitución §12.4).
/// Su responsabilidad es ejecutar con <c>ExecuteUpdate</c> y <c>ExecuteDelete</c>, saltándose el
/// filtro de aislamiento porque cruza los clubes de una sola cuenta, los pasos que la cascada de la
/// base de datos no hace: reescribir los nombres copiados, anular invitaciones y borrar primero lo
/// que bloquea el borrado de los integrantes, y, de los pagos (spec 015), reescribir el nombre de
/// quien los registró y borrar los soportes de los jugadores eliminados.
/// No contiene reglas de negocio ni abre su propia transacción: va dentro de la del servicio.
/// </summary>
public class RepositorioEliminacionCuenta : IRepositorioEliminacionCuenta
{
    private const string Nombre = NombresDeAutoria.PersonaEliminada;

    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioEliminacionCuenta(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AnonimizarAutoriaAsync(
        Guid usuarioId, IReadOnlyCollection<Guid> integranteIds, CancellationToken cancelacion = default)
    {
        var propios = integranteIds.ToArray();

        await _contexto.Comunicados.IgnoreQueryFilters()
            .Where(c => c.PublicadoPorUsuarioRolId != null && propios.Contains(c.PublicadoPorUsuarioRolId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.PublicadoPorNombre, Nombre), cancelacion);
        await _contexto.Comunicados.IgnoreQueryFilters()
            .Where(c => c.RetiradoPorUsuarioRolId != null && propios.Contains(c.RetiradoPorUsuarioRolId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RetiradoPorNombre, Nombre), cancelacion);

        await _contexto.Partidos.IgnoreQueryFilters()
            .Where(p => p.PublicadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PublicadoPorNombre, Nombre), cancelacion);
        await _contexto.Partidos.IgnoreQueryFilters()
            .Where(p => p.ResultadoCambiadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ResultadoCambiadoPorNombre, Nombre), cancelacion);

        // Las filas de los propios integrantes se borran en el mismo paso: no se reescriben.
        await _contexto.PartidosJugador.IgnoreQueryFilters()
            .Where(f => f.IndicadoPorUsuarioId == usuarioId && !propios.Contains(f.UsuarioRolId))
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IndicadoPorNombre, Nombre), cancelacion);

        await _contexto.CartasJugador.IgnoreQueryFilters()
            .Where(c => c.EstadisticasCambiadasPorUsuarioId == usuarioId && !propios.Contains(c.UsuarioRolId))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.EstadisticasCambiadasPorNombre, Nombre), cancelacion);
        await _contexto.CartasJugador.IgnoreQueryFilters()
            .Where(c => c.DorsalCambiadoPorUsuarioId == usuarioId && !propios.Contains(c.UsuarioRolId))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.DorsalCambiadoPorNombre, Nombre), cancelacion);

        await _contexto.FichasJugador.IgnoreQueryFilters()
            .Where(f => f.UltimoCambioPorUsuarioId == usuarioId && !propios.Contains(f.UsuarioRolId))
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.UltimoCambioPorNombre, Nombre), cancelacion);

        // Los pagos se conservan (spec 015, §16.3): solo cambia el nombre de quien los registró,
        // confirmó, rechazó o anuló, que queda copiado en cada paso.
        await _contexto.Pagos.IgnoreQueryFilters()
            .Where(p => p.CreadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreadoPorNombre, Nombre), cancelacion);
        await _contexto.Pagos.IgnoreQueryFilters()
            .Where(p => p.ConfirmadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ConfirmadoPorNombre, Nombre), cancelacion);
        await _contexto.Pagos.IgnoreQueryFilters()
            .Where(p => p.RechazadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.RechazadoPorNombre, Nombre), cancelacion);
        await _contexto.Pagos.IgnoreQueryFilters()
            .Where(p => p.AnuladoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AnuladoPorNombre, Nombre), cancelacion);

        await _contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(i => i.AprobadoPorUsuarioId == usuarioId && !propios.Contains(i.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AprobadoPorNombre, Nombre), cancelacion);
        await _contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(i => i.RetiradoPorUsuarioId == usuarioId && !propios.Contains(i.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.RetiradoPorNombre, Nombre), cancelacion);
    }

    /// <inheritdoc />
    public async Task AnularInvitacionesAsync(
        Guid usuarioId, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        await _contexto.Invitaciones.IgnoreQueryFilters()
            .Where(i => i.CreadaPorUsuarioId == usuarioId && i.UsadaEn == null && i.AnuladaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AnuladaEn, ahoraUtc), cancelacion);

    /// <inheritdoc />
    public async Task EliminarCuentaYSusIntegrantesAsync(
        Guid usuarioId, IReadOnlyCollection<Guid> integranteIds, CancellationToken cancelacion = default)
    {
        var propios = integranteIds.ToArray();

        // Estas dos no caen en cascada (NoAction) y bloquearían el borrado de los integrantes.
        await _contexto.PartidosJugador.IgnoreQueryFilters()
            .Where(f => propios.Contains(f.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);
        await _contexto.AsistenciasEntrenamiento.IgnoreQueryFilters()
            .Where(a => propios.Contains(a.UsuarioRolId))
            .ExecuteDeleteAsync(cancelacion);

        // La imagen de soporte de un pago es de la familia: no sobrevive a su cuenta. El pago sí se
        // conserva, sin jugador y con su nombre y su documento copiados (spec 015, data-model).
        await _contexto.SoportesPago.IgnoreQueryFilters()
            .Where(soporte => _contexto.Pagos.IgnoreQueryFilters()
                .Any(pago => pago.Id == soporte.PagoId && pago.UsuarioRolId != null && propios.Contains(pago.UsuarioRolId.Value)))
            .ExecuteDeleteAsync(cancelacion);

        await _contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(i => i.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(cancelacion);
        await _contexto.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync(cancelacion);

        _contexto.ChangeTracker.Clear();
    }
}
