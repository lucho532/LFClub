using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios.Plataforma;

/// <summary>
/// Representa el acceso a la fila única de configuración del cobro de la plataforma (spec 017).
/// Su responsabilidad es leerla con seguimiento de cambios, bloquearla al guardar y crearla con los
/// valores por defecto si faltara.
/// No guarda llaves ni secretos de la pasarela.
/// </summary>
public class RepositorioConfiguracionPlataforma : IRepositorioConfiguracionPlataforma
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioConfiguracionPlataforma(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ConfiguracionPlataforma> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var configuracion = await _contexto.ConfiguracionPlataforma.FirstOrDefaultAsync(cancelacion);
        if (configuracion is not null)
        {
            return configuracion;
        }

        configuracion = new ConfiguracionPlataforma { ActualizadoEn = DateTime.UtcNow };
        _contexto.ConfiguracionPlataforma.Add(configuracion);
        return configuracion;
    }

    /// <inheritdoc />
    public async Task<ConfiguracionPlataforma> ObtenerBloqueandoAsync(CancellationToken cancelacion = default)
    {
        var configuracion = await _contexto.ConfiguracionPlataforma
            .FromSqlRaw("SELECT * FROM \"ConfiguracionPlataforma\" WHERE \"Id\" = 1 FOR UPDATE")
            .FirstOrDefaultAsync(cancelacion);

        return configuracion ?? await ObtenerAsync(cancelacion);
    }
}
