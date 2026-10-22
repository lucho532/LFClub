using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a la fila única de configuración del cobro de la plataforma (spec 017).
/// Su responsabilidad es leerla, con seguimiento de cambios, y bloquearla mientras se guarda. Si la
/// fila no existe todavía la crea con los valores por defecto.
/// No guarda llaves ni secretos de la pasarela.
/// </summary>
public interface IRepositorioConfiguracionPlataforma
{
    /// <summary>La configuración vigente.</summary>
    Task<ConfiguracionPlataforma> ObtenerAsync(CancellationToken cancelacion = default);

    /// <summary>La configuración vigente, bloqueada hasta el final de la transacción.</summary>
    Task<ConfiguracionPlataforma> ObtenerBloqueandoAsync(CancellationToken cancelacion = default);
}
