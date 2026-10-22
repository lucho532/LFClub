using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Enumeraciones;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a las cifras del club con Entity Framework (spec 015).
/// Su responsabilidad es leer lo aplicado en un mes con una sola consulta que une aplicaciones, pagos
/// confirmados y cargos, y los nombres de jugadores y categorías con consultas por identificador, siempre
/// con el filtro de aislamiento activo: ninguna cifra de otro club puede entrar.
/// No guarda totales, no calcula saldos y no se salta el filtro de aislamiento.
/// </summary>
public class RepositorioFinanzas : IRepositorioFinanzas
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioFinanzas(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AplicacionDelMes>> AplicacionesConfirmadasAsync(
        DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancelacion = default) =>
        await (from aplicacion in _contexto.AplicacionesPago
               join pago in _contexto.Pagos on aplicacion.PagoId equals pago.Id
               join cargo in _contexto.Cargos on aplicacion.CargoId equals cargo.Id
               where pago.Estado == EstadoPago.CONFIRMADO && pago.ConfirmadoEn >= desdeUtc && pago.ConfirmadoEn < hastaUtc
               select new AplicacionDelMes(cargo.CategoriaId, aplicacion.Valor))
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, JugadorDeFinanzas>> JugadoresAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
        usuarioRolIds.Count == 0
            ? new Dictionary<Guid, JugadorDeFinanzas>()
            : await _contexto.UsuariosRol.AsNoTracking()
                .Where(jugador => usuarioRolIds.Contains(jugador.Id))
                .Select(jugador => new { jugador.Id, jugador.Nombres, jugador.Apellidos, jugador.Activo })
                .ToDictionaryAsync(
                    fila => fila.Id, fila => new JugadorDeFinanzas($"{fila.Nombres} {fila.Apellidos}", fila.Activo), cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> AniosDeCategoriasAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default) =>
        categoriaIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _contexto.Categorias.AsNoTracking()
                .Where(categoria => categoriaIds.Contains(categoria.Id))
                .ToDictionaryAsync(categoria => categoria.Id, categoria => categoria.Anio, cancelacion);
}
