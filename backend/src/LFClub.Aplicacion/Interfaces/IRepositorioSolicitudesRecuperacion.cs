using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las solicitudes de recuperación de contraseña.
/// Su responsabilidad es guardarlas, encontrarlas por el hash de su token y dejar sin efecto las
/// anteriores de una cuenta.
/// No decide si una solicitud está vigente ni cambia contraseñas.
/// </summary>
public interface IRepositorioSolicitudesRecuperacion
{
    /// <summary>Busca una solicitud por el hash de su token, con su cuenta.</summary>
    Task<SolicitudRecuperacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancelacion = default);

    /// <summary>Marca como usadas las solicitudes sin usar de la cuenta.</summary>
    Task AnularPendientesAsync(Guid usuarioId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Marca la solicitud como usada solo si no lo estaba; devuelve si la marcó.</summary>
    Task<bool> MarcarUsadaAsync(Guid solicitudId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Añade una solicitud nueva; se guarda con la unidad de trabajo.</summary>
    void Agregar(SolicitudRecuperacion solicitud);
}
