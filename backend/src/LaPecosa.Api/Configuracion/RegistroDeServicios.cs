using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Infraestructura.Correo;
using LaPecosa.Infraestructura.Datos;
using LaPecosa.Infraestructura.Repositorios;
using LaPecosa.Infraestructura.Repositorios.Plataforma;
using LaPecosa.Infraestructura.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Api.Configuracion;

/// <summary>
/// Representa el registro de las dependencias de Infraestructura.
/// Su responsabilidad es conectar cada interfaz de Aplicacion con su implementación: base de datos,
/// repositorios, seguridad y correo.
/// No registra los casos de uso (ver <see cref="RegistroDeCasosDeUso"/>) ni contiene lógica.
/// </summary>
public static class RegistroDeServicios
{
    /// <summary>Registra la base de datos, los repositorios, la seguridad y el correo.</summary>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddDbContext<ContextoLaPecosa>(opciones =>
            opciones.UseNpgsql(configuracion.GetConnectionString("LaPecosa")));

        servicios.AddScoped<IContextoClub, ContextoClub>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<CuentaInicial>();
        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton<IHashContrasena, HashContrasena>();
        servicios.AddSingleton<IEmisorTokenSesion, EmisorTokenSesion>();

        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRepositorioSolicitudesRecuperacion, RepositorioSolicitudesRecuperacion>();
        servicios.AddScoped<IRepositorioPertenencias, RepositorioPertenencias>();
        servicios.AddScoped<IRepositorioRetiroEntreClubes, RepositorioRetiroEntreClubes>();
        servicios.AddScoped<IRepositorioClub, RepositorioClub>();
        servicios.AddScoped<IRepositorioEscudos, RepositorioEscudos>();
        servicios.AddScoped<IRepositorioFotosPerfil, RepositorioFotosPerfil>();
        servicios.AddScoped<IRepositorioInvitacionesPorToken, RepositorioInvitacionesPorToken>();
        servicios.AddScoped<IRepositorioClubesPlataforma, RepositorioClubesPlataforma>();
        servicios.AddScoped<IRepositorioInvitacionesPlataforma, RepositorioInvitacionesPlataforma>();
        servicios.AddScoped<IRepositorioInvitacionesClub, RepositorioInvitacionesClub>();
        servicios.AddScoped<IRepositorioIngresos, RepositorioIngresos>();
        servicios.AddScoped<IRepositorioCategorias, RepositorioCategorias>();
        servicios.AddScoped<IRepositorioJugadores, RepositorioJugadores>();
        servicios.AddScoped<IRepositorioEquipos, RepositorioEquipos>();
        servicios.AddScoped<IRepositorioAsignaciones, RepositorioAsignaciones>();
        servicios.AddScoped<IRepositorioFichas, RepositorioFichas>();
        servicios.AddScoped<IRepositorioDocumentosJugador, RepositorioDocumentosJugador>();
        servicios.AddScoped<IRepositorioFotosJugador, RepositorioFotosJugador>();
        servicios.AddScoped<IRepositorioResumenClub, RepositorioResumenClub>();
        servicios.AddScoped<IRepositorioFichaBasica, RepositorioFichaBasica>();
        servicios.AddScoped<IRepositorioEnlaces, RepositorioEnlaces>();
        servicios.AddScoped<IRepositorioEntrenamientos, RepositorioEntrenamientos>();
        servicios.AddScoped<IRepositorioPartidos, RepositorioPartidos>();
        servicios.AddScoped<IRepositorioConvocatoria, RepositorioConvocatoria>();
        servicios.AddScoped<IRepositorioAsistencias, RepositorioAsistencias>();
        servicios.AddScoped<IRepositorioEstadisticas, RepositorioEstadisticas>();

        servicios.Configure<OpcionesCorreo>(opciones =>
        {
            opciones.Llave = configuracion["Brevo:Llave"] ?? string.Empty;
            opciones.Remitente = configuracion["Brevo:Remitente"] ?? string.Empty;
            opciones.UrlBaseFrontend = (configuracion["Frontend:UrlBase"] ?? string.Empty).TrimEnd('/');
        });

        // Sin llave de Brevo los correos se escriben en el registro (research §9).
        if (string.IsNullOrWhiteSpace(configuracion["Brevo:Llave"]))
        {
            servicios.AddScoped<IServicioCorreo, ServicioCorreoRegistro>();
        }
        else
        {
            servicios.AddHttpClient<IServicioCorreo, ServicioCorreoBrevo>(
                cliente => cliente.Timeout = TimeSpan.FromSeconds(15));
        }

        return servicios;
    }
}
