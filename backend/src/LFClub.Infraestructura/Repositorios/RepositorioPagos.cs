using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los pagos con Entity Framework (spec 015).
/// Su responsabilidad es leer los pagos de un jugador y saber cuáles tienen soporte sin traer los
/// bytes de la imagen, trabajando siempre con el filtro de aislamiento activo.
/// No decide quién puede ver un pago, no calcula saldos y no se salta el filtro de aislamiento.
/// </summary>
public class RepositorioPagos : IRepositorioPagos
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioPagos(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pago>> ListarDelJugadorAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default) =>
        await _contexto.Pagos.AsNoTracking()
            .Where(pago => pago.UsuarioRolId == usuarioRolId)
            .OrderByDescending(pago => pago.CreadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> IdsConSoporteAsync(
        IReadOnlyCollection<Guid> pagoIds, CancellationToken cancelacion = default)
    {
        if (pagoIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        return (await _contexto.SoportesPago
            .Where(soporte => pagoIds.Contains(soporte.PagoId))
            .Select(soporte => soporte.PagoId)
            .ToListAsync(cancelacion)).ToHashSet();
    }

    /// <inheritdoc />
    public async Task<bool> BloquearJugadorAsync(
        Guid clubId, Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"UsuariosRol\" WHERE \"Id\" = {usuarioRolId} AND \"ClubId\" = {clubId} FOR UPDATE",
            cancelacion);
        return await _contexto.UsuariosRol.AsNoTracking().AnyAsync(jugador => jugador.Id == usuarioRolId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<int> SiguienteReciboAsync(Guid clubId, CancellationToken cancelacion = default)
    {
        // El UPDATE deja la fila del club bloqueada hasta el final de la transacción: la lectura que sigue
        // ve el número de esta transacción y ningún otro la puede cambiar mientras tanto.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Clubes\" SET \"UltimoRecibo\" = \"UltimoRecibo\" + 1 WHERE \"Id\" = {clubId}", cancelacion);
        return await _contexto.Clubes.AsNoTracking()
            .Where(club => club.Id == clubId)
            .Select(club => club.UltimoRecibo)
            .SingleAsync(cancelacion);
    }

    /// <inheritdoc />
    public void Agregar(Pago pago) => _contexto.Pagos.Add(pago);

    /// <inheritdoc />
    public Task<Pago?> ObtenerAsync(Guid pagoId, CancellationToken cancelacion = default) =>
        _contexto.Pagos.Include(pago => pago.Aplicaciones).FirstOrDefaultAsync(pago => pago.Id == pagoId, cancelacion);

    /// <inheritdoc />
    public Task<Pago?> ObtenerSinSeguirAsync(Guid pagoId, CancellationToken cancelacion = default) =>
        _contexto.Pagos.AsNoTracking().FirstOrDefaultAsync(pago => pago.Id == pagoId, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pago>> ListarEnRevisionAsync(CancellationToken cancelacion = default) =>
        await _contexto.Pagos.AsNoTracking()
            .Where(pago => pago.Estado == EstadoPago.EN_REVISION)
            .OrderBy(pago => pago.CreadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<int> ContarEnRevisionAsync(CancellationToken cancelacion = default) =>
        _contexto.Pagos.CountAsync(pago => pago.Estado == EstadoPago.EN_REVISION, cancelacion);

    /// <inheritdoc />
    public void AgregarSoporte(SoportePago soporte) => _contexto.SoportesPago.Add(soporte);

    /// <inheritdoc />
    public Task<SoportePago?> ObtenerSoporteAsync(Guid pagoId, CancellationToken cancelacion = default) =>
        _contexto.SoportesPago.AsNoTracking().FirstOrDefaultAsync(soporte => soporte.PagoId == pagoId, cancelacion);
}
