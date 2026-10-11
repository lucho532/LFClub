using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Infraestructura.Datos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace LaPecosa.Pruebas.Integracion.Base;

/// <summary>
/// Arranca la API real contra un PostgreSQL 17 en un contenedor, con las migraciones aplicadas y
/// el correo sustituido por <see cref="CorreoEnMemoria"/>. La comparten todas las pruebas.
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CorreoDesarrollador = "desarrollador@lapecosa.test";

    private readonly PostgreSqlContainer _baseDatos = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("lapecosa")
        .Build();

    public CorreoEnMemoria Correo { get; } = new();

    public RelojDePrueba Reloj { get; } = new();

    public SembradorActividades Actividades => new(this);

    public Sembrador Sembrador => new(this);

    public SembradorCategorias Categorias => new(this);

    public SembradorFichas Fichas => new(this);

    public SembradorHermanos Hermanos => new(this);

    public SembradorJugadores Jugadores => new(this);

    public SembradorPersonas Personas => new(this);

    public async Task InitializeAsync()
    {
        await _baseDatos.StartAsync();
        _ = Services;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _baseDatos.DisposeAsync();
    }

    /// <summary>Cliente sin sesión.</summary>
    public ClienteDePrueba CrearClienteDePrueba() => new(this, CreateClient());

    /// <summary>Ejecuta una operación con el contexto de datos, fuera de cualquier petición.</summary>
    public async Task<T> ConContextoAsync<T>(Func<ContextoLaPecosa, Task<T>> operacion)
    {
        await using var ambito = Services.CreateAsyncScope();
        return await operacion(ambito.ServiceProvider.GetRequiredService<ContextoLaPecosa>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:LaPecosa", _baseDatos.GetConnectionString());
        builder.UseSetting("Plataforma:CorreoDesarrollador", CorreoDesarrollador);
        builder.UseSetting("Sesion:ClaveFirma", "clave-de-firma-solo-para-las-pruebas-de-integracion");
        builder.UseSetting("Sesion:DiasVigencia", "7");
        builder.UseSetting("Brevo:Llave", string.Empty);
        builder.UseSetting("Frontend:UrlBase", "http://localhost:5173");
        builder.UseSetting("BaseDatos:MigrarAlArrancar", "true");
        builder.UseSetting("Limites:AnonimoPorMinuto", "100000");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<IServicioCorreo>();
            servicios.AddSingleton<IServicioCorreo>(Correo);
            servicios.RemoveAll<IReloj>();
            servicios.AddSingleton<IReloj>(Reloj);
        });
    }
}

/// <summary>Colección de xUnit que comparte una sola API y una sola base de datos.</summary>
[CollectionDefinition(Nombre)]
public class ColeccionApi : ICollectionFixture<FabricaApi>
{
    public const string Nombre = "Api";
}
