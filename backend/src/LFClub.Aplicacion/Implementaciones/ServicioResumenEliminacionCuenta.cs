using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa la lectura de lo que se eliminaría con una cuenta (spec 014, RF-003 y RF-009).
/// Su responsabilidad es listar por nombre y club a todos los jugadores de la cuenta, también con
/// una sesión limitada por documento (research §5), y calcular con
/// <see cref="ReglaEliminacionDeCuenta"/> los clubes que impiden eliminarla.
/// No bloquea filas ni borra: la decisión definitiva es la de <see cref="ServicioEliminacionCuenta"/>
/// dentro de su transacción. No accede al contexto de Entity Framework.
/// </summary>
public class ServicioResumenEliminacionCuenta
{
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioClubesPlataforma _clubes;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioResumenEliminacionCuenta(
        IRepositorioUsuarios usuarios, IRepositorioPertenencias pertenencias, IRepositorioClubesPlataforma clubes)
    {
        _usuarios = usuarios;
        _pertenencias = pertenencias;
        _clubes = clubes;
    }

    /// <summary>Resumen de la cuenta; 404 para la cuenta DESARROLLADOR o inexistente.</summary>
    public async Task<ResumenEliminacionCuentaDto> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        var cuenta = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion);
        if (cuenta is null || cuenta.EsDesarrollador)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var integrantes = await _pertenencias.ListarDeUsuarioAsync(usuarioId, cancelacion);

        var presidentes = new Dictionary<Guid, int>();
        foreach (var clubId in integrantes.Where(i => i.Rol == Rol.PRESIDENTE).Select(i => i.ClubId).Distinct())
        {
            presidentes[clubId] = await _clubes.ContarPresidentesAsync(clubId, cancelacion);
        }

        var bloqueantes = ReglaEliminacionDeCuenta.ClubesBloqueantes(integrantes, presidentes);

        return new ResumenEliminacionCuentaDto(
            bloqueantes.Count == 0,
            integrantes.Where(i => i.Rol == Rol.JUGADOR).Select(ComoJugador).ToList(),
            ClubesQueImpiden(integrantes, bloqueantes));
    }

    /// <summary>Los clubes bloqueantes con su nombre, tomado del integrante que los trae cargados.</summary>
    internal static List<ClubQueImpideDto> ClubesQueImpiden(
        IEnumerable<UsuarioRol> integrantes, IReadOnlyList<Guid> bloqueantes) =>
        bloqueantes
            .Select(clubId => new ClubQueImpideDto(
                clubId, integrantes.First(i => i.ClubId == clubId).Club?.Nombre ?? string.Empty))
            .ToList();

    private static JugadorQueSeEliminaDto ComoJugador(UsuarioRol jugador) => new(
        $"{jugador.Nombres} {jugador.Apellidos}".Trim(),
        jugador.Club?.Nombre ?? string.Empty,
        !jugador.Activo,
        jugador.EstadoIngreso == EstadoIngreso.EN_ESPERA);
}
