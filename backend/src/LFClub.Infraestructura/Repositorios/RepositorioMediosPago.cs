using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso a los medios de pago del club con Entity Framework (spec 015).
/// Su responsabilidad es listar, leer, añadir y quitar medios, trabajando siempre con el filtro de
/// aislamiento activo.
/// No decide quién los ve, no toca los pagos hechos con esa entidad (cada pago guarda su entidad como
/// texto) y no se salta el filtro de aislamiento.
/// </summary>
public class RepositorioMediosPago : IRepositorioMediosPago
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioMediosPago(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MedioPagoClub>> ListarAsync(bool soloActivos, CancellationToken cancelacion = default) =>
        await _contexto.MediosPagoClub.AsNoTracking()
            .Where(medio => !soloActivos || medio.Activo)
            .OrderBy(medio => medio.Entidad)
            .ThenBy(medio => medio.CreadoEn)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<MedioPagoClub?> ObtenerAsync(Guid medioId, CancellationToken cancelacion = default) =>
        _contexto.MediosPagoClub.FirstOrDefaultAsync(medio => medio.Id == medioId, cancelacion);

    /// <inheritdoc />
    public void Agregar(MedioPagoClub medio) => _contexto.MediosPagoClub.Add(medio);

    /// <inheritdoc />
    public void Quitar(MedioPagoClub medio) => _contexto.MediosPagoClub.Remove(medio);
}
