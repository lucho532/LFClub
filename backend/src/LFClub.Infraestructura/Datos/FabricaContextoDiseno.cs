using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LFClub.Infraestructura.Datos;

/// <summary>
/// Representa la fábrica que usan las herramientas de Entity Framework para crear migraciones.
/// Su responsabilidad es construir el contexto sin arrancar la API.
/// No se usa al ejecutar la aplicación ni se conecta a ninguna base de datos real.
/// </summary>
public class FabricaContextoDiseno : IDesignTimeDbContextFactory<ContextoLFClub>
{
    /// <inheritdoc />
    public ContextoLFClub CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ContextoLFClub>()
            .UseNpgsql("Host=localhost;Database=lfclub")
            .Options;

        return new ContextoLFClub(opciones, new ContextoClub());
    }
}
