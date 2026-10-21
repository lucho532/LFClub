using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los medios por los que el club recibe pagos (spec 015, RF-026 a RF-028).
/// Su responsabilidad es listar, leer, añadir y quitar los medios del club de la petición.
/// No decide quién los ve ni los cambia: eso lo hace el servicio.
/// </summary>
public interface IRepositorioMediosPago
{
    /// <summary>Los medios del club, por entidad y número; solo los activos si se indica. No los sigue.</summary>
    Task<IReadOnlyList<MedioPagoClub>> ListarAsync(bool soloActivos, CancellationToken cancelacion = default);

    /// <summary>Un medio del club, o nulo; lo sigue para poder cambiarlo.</summary>
    Task<MedioPagoClub?> ObtenerAsync(Guid medioId, CancellationToken cancelacion = default);

    /// <summary>Añade un medio nuevo, para guardarlo con la unidad de trabajo.</summary>
    void Agregar(MedioPagoClub medio);

    /// <summary>Quita un medio, para guardarlo con la unidad de trabajo. No toca ningún pago.</summary>
    void Quitar(MedioPagoClub medio);
}
