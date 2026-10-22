using LFClub.Aplicacion.Implementaciones;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Api.Configuracion;

/// <summary>
/// Representa el registro de los casos de uso de Aplicacion.
/// Su responsabilidad es conectar cada <c>IServicio</c> con su implementación y registrar los
/// colaboradores que comparten varios casos de uso.
/// No registra infraestructura ni contiene lógica.
/// </summary>
public static class RegistroDeCasosDeUso
{
    /// <summary>Registra los servicios de los casos de uso.</summary>
    public static IServiceCollection AgregarCasosDeUso(this IServiceCollection servicios)
    {
        servicios.AddScoped<IServicioSuspensionPorImpago, ServicioSuspensionPorImpago>();
        servicios.AddScoped<IServicioTarifas, ServicioTarifas>();
        servicios.AddScoped<IServicioCobrosPlataforma, ServicioCobrosPlataforma>();
        servicios.AddScoped<IServicioInicioDelCobro, ServicioInicioDelCobro>();
        servicios.AddScoped<IServicioConsultaPagoPlataforma, ServicioConsultaPagoPlataforma>();
        servicios.AddScoped<IServicioPagoPlataforma, ServicioPagoPlataforma>();
        servicios.AddScoped<IServicioNotificacionPasarela, ServicioNotificacionPasarela>();
        servicios.AddScoped<IServicioCobrosDeUnClub, ServicioCobrosDeUnClub>();
        servicios.AddScoped<IServicioConsultaNotificaciones, ServicioConsultaNotificaciones>();
        servicios.AddScoped<IServicioGeneracionCargos, ServicioGeneracionCargos>();
        servicios.AddScoped<IServicioMensualidad, ServicioMensualidad>();
        servicios.AddScoped<IServicioCuentaDelJugador, ServicioCuentaDelJugador>();
        servicios.AddScoped<IServicioAvisoDeMora, ServicioAvisoDeMora>();
        servicios.AddScoped<AplicadorDePagos>();
        servicios.AddScoped<IServicioRegistroDePagos, ServicioRegistroDePagos>();
        servicios.AddScoped<IServicioConsultaDePagos, ServicioConsultaDePagos>();
        servicios.AddScoped<IServicioMediosPago, ServicioMediosPago>();
        servicios.AddScoped<IServicioRevisionDePagos, ServicioRevisionDePagos>();
        servicios.AddScoped<IServicioAnulacionDePagos, ServicioAnulacionDePagos>();
        servicios.AddScoped<IServicioFinanzas, ServicioFinanzas>();
        servicios.AddScoped<IServicioSesion, ServicioSesion>();
        servicios.AddScoped<IServicioRecuperacion, ServicioRecuperacion>();
        servicios.AddScoped<IServicioFotoPerfil, ServicioFotoPerfil>();

        servicios.AddScoped<IServicioConsultaClubes, ServicioConsultaClubes>();
        servicios.AddScoped<IServicioCreacionClub, ServicioCreacionClub>();
        servicios.AddScoped<IServicioInvitacionPresidente, ServicioInvitacionPresidente>();
        servicios.AddScoped<IServicioRetiroPresidente, ServicioRetiroPresidente>();
        servicios.AddScoped<IServicioIdentidadClub, ServicioIdentidadClub>();
        servicios.AddScoped<IServicioEstadoClub, ServicioEstadoClub>();
        servicios.AddScoped<IServicioEliminacionClub, ServicioEliminacionClub>();
        servicios.AddScoped<IServicioEscudoPublico, ServicioEscudoPublico>();

        servicios.AddScoped<IServicioConsultaClub, ServicioConsultaClub>();
        servicios.AddScoped<IServicioConfiguracionClub, ServicioConfiguracionClub>();
        servicios.AddScoped<IServicioRegistroConInvitacion, ServicioRegistroConInvitacion>();
        servicios.AddScoped<IServicioAceptacionInvitacion, ServicioAceptacionInvitacion>();

        servicios.AddScoped<IServicioInvitacionesClub, ServicioInvitacionesClub>();
        servicios.AddScoped<IServicioConsultaIngresos, ServicioConsultaIngresos>();
        servicios.AddScoped<IServicioAprobacionIngreso, ServicioAprobacionIngreso>();
        servicios.AddScoped<IServicioRechazoIngreso, ServicioRechazoIngreso>();
        servicios.AddScoped<EliminadorDeCuentaSinClub>();

        servicios.AddScoped<UbicadorDeJugadores>();
        servicios.AddScoped<LectorDeCategorias>();
        servicios.AddScoped<IServicioCategorias, ServicioCategorias>();
        servicios.AddScoped<IServicioConsultaCategorias, ServicioConsultaCategorias>();
        servicios.AddScoped<IServicioEntrenadoresDeCategoria, ServicioEntrenadoresDeCategoria>();
        servicios.AddScoped<IServicioUbicacionJugador, ServicioUbicacionJugador>();
        servicios.AddScoped<IServicioEquipos, ServicioEquipos>();
        servicios.AddScoped<IServicioJugadoresDeEquipo, ServicioJugadoresDeEquipo>();
        servicios.AddScoped<IServicioRetiroJugador, ServicioRetiroJugador>();
        servicios.AddScoped<IServicioMiCategoria, ServicioMiCategoria>();

        servicios.AddScoped<AccesoAFicha>();
        servicios.AddScoped<IServicioConsultaFicha, ServicioConsultaFicha>();
        servicios.AddScoped<IServicioFichaJugador, ServicioFichaJugador>();
        servicios.AddScoped<IServicioDocumentosJugador, ServicioDocumentosJugador>();
        servicios.AddScoped<IServicioIdentidadJugador, ServicioIdentidadJugador>();

        servicios.AddScoped<IServicioAgregarHermano, ServicioAgregarHermano>();

        servicios.AddScoped<IServicioListaJugadores, ServicioListaJugadores>();
        servicios.AddScoped<IServicioFotoJugador, ServicioFotoJugador>();
        servicios.AddScoped<IServicioResumenClub, ServicioResumenClub>();

        servicios.AddScoped<IServicioFichaBasica, ServicioFichaBasica>();
        servicios.AddScoped<ResolutorDeEnlaces>();
        servicios.AddScoped<IServicioDatosPropios, ServicioDatosPropios>();
        servicios.AddScoped<ServicioResumenEliminacionCuenta>();
        servicios.AddScoped<IServicioEliminacionCuenta, ServicioEliminacionCuenta>();

        servicios.AddScoped<AccesoAActividad>();
        servicios.AddScoped<ArmadoDeAudiencia>();
        servicios.AddScoped<IServicioEntrenamientos, ServicioEntrenamientos>();
        servicios.AddScoped<IServicioConsultaEntrenamientos, ServicioConsultaEntrenamientos>();
        servicios.AddScoped<IServicioAudienciaDeEntrenamiento, ServicioAudienciaDeEntrenamiento>();
        servicios.AddScoped<IServicioJugadoresSumables, ServicioJugadoresSumables>();
        servicios.AddScoped<IServicioCalendario, ServicioCalendario>();
        servicios.AddScoped<IServicioPartidos, ServicioPartidos>();
        servicios.AddScoped<IServicioConsultaPartidos, ServicioConsultaPartidos>();
        servicios.AddScoped<IServicioConvocatoria, ServicioConvocatoria>();
        servicios.AddScoped<IServicioResultado, ServicioResultado>();
        servicios.AddScoped<IServicioResultadoFamilia, ServicioResultadoFamilia>();
        servicios.AddScoped<IServicioAsistencia, ServicioAsistencia>();
        servicios.AddScoped<IServicioRespuestas, ServicioRespuestas>();
        servicios.AddScoped<IServicioEstadisticas, ServicioEstadisticas>();

        servicios.AddScoped<AccesoACarta>();
        servicios.AddScoped<IServicioConsultaCarta, ServicioConsultaCarta>();
        servicios.AddScoped<IServicioCarta, ServicioCarta>();
        servicios.AddScoped<IServicioImagenCarta, ServicioImagenCarta>();

        servicios.AddScoped<IServicioPublicacionComunicados, ServicioPublicacionComunicados>();
        servicios.AddScoped<IServicioConsultaComunicados, ServicioConsultaComunicados>();
        servicios.AddScoped<IServicioLecturaComunicados, ServicioLecturaComunicados>();
        servicios.AddScoped<IServicioConsultaCampana, ServicioConsultaCampana>();
        servicios.AddScoped<IServicioLecturaCampana, ServicioLecturaCampana>();
        servicios.AddScoped<IServicioAvisosDeActividad, ServicioAvisosDeActividad>();

        return servicios;
    }
}
