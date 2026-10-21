using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los cargos con Entity Framework y SQL (spec 015).
/// Su responsabilidad es generar la mensualidad del mes con una sola sentencia
/// <c>INSERT … SELECT … ON CONFLICT DO NOTHING</c>, que es idempotente gracias al índice único de
/// una mensualidad por jugador y mes, de modo que dos ejecuciones o dos instancias de la API a la vez
/// no duplican nada (CE-001). La generación cruza los clubes: es una operación del sistema, sin ruta,
/// que solo escribe cargos y no devuelve datos de ningún club (constitución §7.1, nota 3 del plan).
/// No decide quién ve los cargos ni calcula saldos, y no crea cargos retroactivos: solo el del mes en
/// curso en Colombia.
/// </summary>
public class RepositorioCargos : IRepositorioCargos
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto.</summary>
    public RepositorioCargos(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<int> GenerarDelMesAsync(DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "Cargos" ("Id", "ClubId", "UsuarioRolId", "Tipo", "Mes", "Valor", "CategoriaId", "CreadoEn")
             SELECT gen_random_uuid(), u."ClubId", u."Id", 'MENSUALIDAD', {ReglaMesDeCobro.MesEnCurso(ahoraUtc)},
                    COALESCE(u."ValorMensualidadPropio", c."ValorMensualidad"), u."CategoriaId", {ahoraUtc}
             FROM "UsuariosRol" u
             JOIN "Clubes" c ON c."Id" = u."ClubId"
             WHERE u."Rol" = 'JUGADOR' AND u."EstadoIngreso" = 'APROBADO' AND u."Activo"
               AND u."CobraDesde" IS NOT NULL AND u."CobraDesde" <= {ReglaMesDeCobro.MesEnCurso(ahoraUtc)}
               AND c."ValorMensualidad" IS NOT NULL AND c."Estado" <> 'DADO_DE_BAJA'
             ON CONFLICT ("UsuarioRolId", "Mes") WHERE "Tipo" = 'MENSUALIDAD' DO NOTHING
             """,
            cancelacion);

    /// <inheritdoc />
    public Task<int> GenerarDelMesDelClubAsync(
        Guid clubId, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
        _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "Cargos" ("Id", "ClubId", "UsuarioRolId", "Tipo", "Mes", "Valor", "CategoriaId", "CreadoEn")
             SELECT gen_random_uuid(), u."ClubId", u."Id", 'MENSUALIDAD', {ReglaMesDeCobro.MesEnCurso(ahoraUtc)},
                    COALESCE(u."ValorMensualidadPropio", c."ValorMensualidad"), u."CategoriaId", {ahoraUtc}
             FROM "UsuariosRol" u
             JOIN "Clubes" c ON c."Id" = u."ClubId"
             WHERE c."Id" = {clubId}
               AND u."Rol" = 'JUGADOR' AND u."EstadoIngreso" = 'APROBADO' AND u."Activo"
               AND u."CobraDesde" IS NOT NULL AND u."CobraDesde" <= {ReglaMesDeCobro.MesEnCurso(ahoraUtc)}
               AND c."ValorMensualidad" IS NOT NULL AND c."Estado" <> 'DADO_DE_BAJA'
             ON CONFLICT ("UsuarioRolId", "Mes") WHERE "Tipo" = 'MENSUALIDAD' DO NOTHING
             """,
            cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioRol>> ListarConValorPropioAsync(CancellationToken cancelacion = default) =>
        await JugadoresActivos()
            .Where(jugador => jugador.ValorMensualidadPropio != null)
            .AsNoTracking()
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<int> FijarValorPropioAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, decimal? valor, CancellationToken cancelacion = default) =>
        await JugadoresActivos()
            .Where(jugador => usuarioRolIds.Contains(jugador.Id))
            .ExecuteUpdateAsync(
                cambios => cambios.SetProperty(jugador => jugador.ValorMensualidadPropio, valor), cancelacion);

    private IQueryable<UsuarioRol> JugadoresActivos() => _contexto.UsuariosRol.Where(
        jugador => jugador.Rol == Rol.JUGADOR && jugador.EstadoIngreso == EstadoIngreso.APROBADO && jugador.Activo);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CargoConAplicado>> ListarConAplicadoAsync(
        Guid? usuarioRolId = null, CancellationToken cancelacion = default) =>
        await _contexto.Cargos
            .Where(cargo => usuarioRolId == null || cargo.UsuarioRolId == usuarioRolId)
            .OrderByDescending(cargo => cargo.Mes)
            .ThenByDescending(cargo => cargo.CreadoEn)
            .Select(cargo => new CargoConAplicado(
                cargo.Id,
                cargo.UsuarioRolId,
                cargo.Tipo,
                cargo.Mes,
                cargo.Valor,
                (from aplicacion in _contexto.AplicacionesPago
                 join pago in _contexto.Pagos on aplicacion.PagoId equals pago.Id
                 where aplicacion.CargoId == cargo.Id && pago.Estado == EstadoPago.CONFIRMADO
                 select (decimal?)aplicacion.Valor).Sum() ?? 0m,
                cargo.CategoriaId))
            .ToListAsync(cancelacion);
}
