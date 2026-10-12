using System.Threading.RateLimiting;
using LaPecosa.Api.Autorizacion;
using LaPecosa.Api.Errores;
using LaPecosa.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LaPecosa.Api.Configuracion;

/// <summary>
/// Representa la configuración de seguridad de la API.
/// Su responsabilidad es registrar la autenticación JWT con comprobación del sello en cada
/// petición, exigir sesión por omisión en todo endpoint (constitución §9), CORS para el frontend y
/// el límite de peticiones de los endpoints anónimos.
/// No decide quién puede usar cada endpoint: eso lo hacen los atributos de autorización.
/// </summary>
public static class ConfiguracionSeguridad
{
    /// <summary>Nombre de la política de límite de peticiones de los endpoints anónimos.</summary>
    public const string PoliticaAnonimo = "anonimo";

    /// <summary>Nombre de la política de CORS del frontend.</summary>
    public const string PoliticaCors = "frontend";

    /// <summary>Registra autenticación, autorización, CORS y límite de peticiones.</summary>
    public static IServiceCollection AgregarSeguridad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var sesion = configuracion.GetSection(OpcionesSesion.Seccion).Get<OpcionesSesion>() ?? new OpcionesSesion();
        if (sesion.ClaveFirma.Length < 32)
        {
            throw new InvalidOperationException("Falta 'Sesion:ClaveFirma' o tiene menos de 32 caracteres.");
        }

        servicios.Configure<OpcionesSesion>(configuracion.GetSection(OpcionesSesion.Seccion));
        servicios.AddScoped<ValidadorSesion>();

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = EmisorTokenSesion.CrearClave(sesion.ClaveFirma),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                opciones.Events = new JwtBearerEvents
                {
                    OnTokenValidated = contexto => contexto.HttpContext.RequestServices
                        .GetRequiredService<ValidadorSesion>().ValidarAsync(contexto),
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        await Problema.SinSesion().EscribirAsync(contexto.HttpContext);
                    },
                };
            });

        servicios.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        var urlFrontend = (configuracion["Frontend:UrlBase"] ?? string.Empty).TrimEnd('/');
        servicios.AddCors(opciones => opciones.AddPolicy(PoliticaCors, politica =>
        {
            if (urlFrontend.Length > 0)
            {
                // Content-Disposition: la pantalla lee el nombre de la imagen de la carta (spec 010).
                politica.WithOrigins(urlFrontend).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
            }
        }));

        var porMinuto = configuracion.GetValue("Limites:AnonimoPorMinuto", 30);
        servicios.AddRateLimiter(opciones =>
        {
            opciones.OnRejected = async (contexto, _) => await new Problema(
                429, "demasiadas_peticiones", "Demasiados intentos. Espera un minuto y vuelve a intentarlo.")
                .EscribirAsync(contexto.HttpContext);

            opciones.AddPolicy(PoliticaAnonimo, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = porMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return servicios;
    }
}
