using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Utilidades;
using LFClub.Infraestructura.Correo;
using LFClub.Infraestructura.Datos;
using LFClub.Infraestructura.Imagenes;
using LFClub.Infraestructura.Pagos;
using LFClub.Infraestructura.Repositorios;
using LFClub.Infraestructura.Repositorios.Plataforma;
using LFClub.Infraestructura.Seguridad;
using LFClub.Infraestructura.Trabajos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Api.Configuracion;

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
        servicios.AddDbContext<ContextoLFClub>(opciones =>
            opciones.UseNpgsql(configuracion.GetConnectionString("LFClub")));

        servicios.AddScoped<IContextoClub, ContextoClub>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<CuentaInicial>();
        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton<IHashContrasena, HashContrasena>();
        servicios.AddSingleton<IEmisorTokenSesion, EmisorTokenSesion>();

        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IRepositorioSolicitudesRecuperacion, RepositorioSolicitudesRecuperacion>();
        servicios.AddScoped<IRepositorioPertenencias, RepositorioPertenencias>();
        servicios.AddScoped<IRepositorioEliminacionCuenta, RepositorioEliminacionCuenta>();
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
        servicios.AddScoped<IRepositorioAudiencia, RepositorioAudiencia>();
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
        servicios.AddScoped<IRepositorioCartas, RepositorioCartas>();
        servicios.AddScoped<IRepositorioComunicados, RepositorioComunicados>();
        servicios.AddScoped<IRepositorioAvisos, RepositorioAvisos>();
        servicios.AddSingleton<IGeneradorImagenCarta, GeneradorImagenCarta>();
        servicios.AddSingleton<IGeneradorImagenResguardo, GeneradorImagenResguardo>();

        servicios.AddScoped<IRepositorioCargos, RepositorioCargos>();
        servicios.AddScoped<IRepositorioPagos, RepositorioPagos>();
        servicios.AddScoped<IRepositorioMediosPago, RepositorioMediosPago>();
        servicios.AddScoped<IRepositorioFinanzas, RepositorioFinanzas>();

        // Cobro del uso de la plataforma (spec 017): repositorios, pasarela y trabajo.
        servicios.AddScoped<IRepositorioCobrosPlataforma, RepositorioCobrosPlataforma>();
        servicios.AddScoped<IRepositorioConfiguracionPlataforma, RepositorioConfiguracionPlataforma>();
        servicios.AddScoped<IRepositorioConteoDeIntegrantes, RepositorioConteoDeIntegrantes>();
        servicios.AddSingleton(new OpcionesWompi
        {
            LlavePublica = configuracion["Wompi:LlavePublica"] ?? string.Empty,
            SecretoIntegridad = configuracion["Wompi:SecretoIntegridad"] ?? string.Empty,
            SecretoEventos = configuracion["Wompi:SecretoEventos"] ?? string.Empty,
            UrlCheckout = configuracion["Wompi:UrlCheckout"] is { Length: > 0 } url ? url : OpcionesWompi.UrlCheckoutPorDefecto,
        });
        servicios.AddSingleton<IPasarelaDePago, PasarelaWompi>();
        servicios.AddSingleton(new OpcionesDeEnlaces
        {
            UrlBaseFrontend = (configuracion["Frontend:UrlBase"] ?? string.Empty).TrimEnd('/'),
        });

        if (configuracion.GetValue("Plataforma:TrabajoActivo", true))
        {
            servicios.AddHostedService<TrabajoCobrosPlataforma>();
        }

        // La mensualidad nace sola cada mes (spec 015); las pruebas que no la necesitan la apagan.
        if (configuracion.GetValue("Cargos:TrabajoActivo", true))
        {
            servicios.AddHostedService<TrabajoCargosMensuales>();
        }

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
