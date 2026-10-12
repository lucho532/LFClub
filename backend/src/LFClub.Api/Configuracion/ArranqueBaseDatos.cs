using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Api.Configuracion;

/// <summary>
/// Representa la preparación de la base de datos al arrancar la API.
/// Su responsabilidad es aplicar las migraciones pendientes, si la configuración lo pide, y crear
/// la cuenta DESARROLLADOR si todavía no existe (research §6).
/// No crea datos de ningún club.
/// </summary>
public static class ArranqueBaseDatos
{
    /// <summary>Aplica migraciones y crea la cuenta inicial.</summary>
    public static async Task PrepararBaseDatosAsync(this WebApplication aplicacion)
    {
        await using var ambito = aplicacion.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<ContextoLFClub>();

        if (aplicacion.Configuration.GetValue("BaseDatos:MigrarAlArrancar", false))
        {
            await contexto.Database.MigrateAsync();
        }

        await ambito.ServiceProvider.GetRequiredService<CuentaInicial>()
            .CrearSiFaltaAsync(aplicacion.Configuration["Plataforma:CorreoDesarrollador"]);
    }
}
