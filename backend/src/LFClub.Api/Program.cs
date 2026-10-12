using LFClub.Api.Configuracion;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AgregarApi()
    .AgregarSeguridad(builder.Configuration)
    .AgregarInfraestructura(builder.Configuration)
    .AgregarCasosDeUso();

var app = builder.Build();

await app.PrepararBaseDatosAsync();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(ConfiguracionSeguridad.PoliticaCors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>
/// Representa el punto de entrada de la API.
/// Su responsabilidad es componer la configuración y arrancar el servidor.
/// No contiene reglas de negocio; se declara para que las pruebas de integración puedan arrancarla.
/// </summary>
public partial class Program;
