using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que genera la mensualidad de cada mes.
/// Su responsabilidad es pedir al repositorio, con el instante del <see cref="IReloj"/>, la
/// generación idempotente del mes en curso, de todos los clubes o de uno.
/// No decide quién debe pagar ni cuánto: eso lo hace la sentencia del repositorio con el valor del club,
/// el valor propio y la fecha desde la que se cobra. No accede al contexto de Entity Framework ni
/// conoce HTTP.
/// </summary>
public class ServicioGeneracionCargos : IServicioGeneracionCargos
{
    private readonly IRepositorioCargos _cargos;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioGeneracionCargos(IRepositorioCargos cargos, IReloj reloj)
    {
        _cargos = cargos;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public Task<int> GenerarAsync(CancellationToken cancelacion = default) =>
        _cargos.GenerarDelMesAsync(_reloj.AhoraUtc, cancelacion);

    /// <inheritdoc />
    public Task<int> GenerarDelClubAsync(Guid clubId, CancellationToken cancelacion = default) =>
        _cargos.GenerarDelMesDelClubAsync(clubId, _reloj.AhoraUtc, cancelacion);
}
