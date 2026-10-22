using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el conteo de integrantes activos por rol que sirve de base al cobro de la plataforma
/// (spec 017, RF-003).
/// Su responsabilidad es contar, de uno o varios clubes, los integrantes de ingreso aprobado y no
/// retirados, con la misma definición que usa el panel de la plataforma.
/// No lee datos personales: solo devuelve números.
/// </summary>
public interface IRepositorioConteoDeIntegrantes
{
    /// <summary>Conteo por rol de cada club pedido; un club sin integrantes aparece con ceros.</summary>
    Task<IReadOnlyDictionary<Guid, ConteoPorRol>> ContarPorRolDeClubesAsync(
        IEnumerable<Guid> clubIds, CancellationToken cancelacion = default);

    /// <summary>Conteo por rol de un club.</summary>
    Task<ConteoPorRol> ContarPorRolAsync(Guid clubId, CancellationToken cancelacion = default);
}
