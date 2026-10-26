using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>
/// La suspensión automática por falta de pago y la vuelta a la normalidad (constitución §7.4, spec 017,
/// historia 3). El trabajo está apagado: cada prueba lo ejecuta a mano con el reloj fijado.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SuspensionPorImpagoPruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public SuspensionPorImpagoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose()
    {
        _fabrica.Reloj.UsarElReal();
        _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();
    }

    private void Fijar(int dia, int hora = 10, int minuto = 0, int mes = 11) =>
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, mes, dia), new TimeOnly(hora, minuto));
    private Task<Club> ClubAsync(Club club) => _fabrica.CobrosPlataforma.ClubAsync(club);
    private Task<CobroPlataforma> CobroVencidoAsync(Club club, DateOnly? periodo = null, decimal total = 50000) =>
        _fabrica.CobrosPlataforma.CrearCobroAsync(club, periodo ?? Noviembre, (periodo ?? Noviembre).AddDays(5), total);

    private async Task PagarPorPasarelaAsync(CobroPlataforma cobro)
    {
        var intento = await _fabrica.CobrosPlataforma.CrearIntentoAsync(cobro);
        var (cuerpo, checksum) = Notificacion(intento.Referencia, cobro.Total);
        var respuesta = await _fabrica.CobrosPlataforma.EnviarNotificacionAsync(cuerpo, checksum);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_cinco_dias_de_gracia_el_club_se_suspende_el_dia_seis_a_las_0000_de_Colombia()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(precio: 1000, dias: 5);
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(club, Noviembre);
        Fijar(1);
        await _fabrica.CobrosPlataforma.GenerarAsync();

        Fijar(5, 23, 59);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.ACTIVO, (await ClubAsync(club)).Estado);
        Fijar(6, 0, 0);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        var suspendido = await ClubAsync(club);
        Assert.Equal(EstadoClub.SUSPENDIDO, suspendido.Estado);
        Assert.Equal(MotivoSuspension.IMPAGO, suspendido.MotivoSuspension);
        Assert.Null(suspendido.EstadoCambiadoPorUsuarioId);
        Assert.NotNull(suspendido.EstadoCambiadoEn);
    }

    [Fact]
    public async Task El_limite_es_las_0500_UTC_del_dia_seis()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await CobroVencidoAsync(club); // SuspendeDesde = 6 de noviembre

        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 6), new TimeOnly(0, 0));
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);

        var otro = await _fabrica.Sembrador.CrearClubAsync();
        await CobroVencidoAsync(otro);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 5), new TimeOnly(23, 59));
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.ACTIVO, (await ClubAsync(otro)).Estado);
    }

    [Fact]
    public async Task El_presidente_entra_y_los_demas_roles_reciben_el_aviso_de_incidencia_sin_datos_del_cobro()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (directivo, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (jugador, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var clientePresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var clienteDirectivo = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(directivo);
        var clienteJugador = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(jugador);
        await CobroVencidoAsync(club);
        Fijar(10);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        Assert.Equal(HttpStatusCode.OK, (await clientePresidente.GetAsync($"/api/clubes/{club.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientePresidente.GetAsync($"/api/clubes/{club.Id}/pago-plataforma")).StatusCode);
        foreach (var cliente in new[] { clienteDirectivo, clienteJugador })
        {
            var respuesta = await cliente.GetAsync($"/api/clubes/{club.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(respuesta));
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain("50000", cuerpo);
            Assert.DoesNotContain("cobro", cuerpo, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Pagar_reactiva_el_club_de_inmediato_y_sus_datos_siguen_intactos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var cobro = await CobroVencidoAsync(club);
        Fijar(10);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);
        var integrantesAntes = await _fabrica.ConContextoAsync(c => c.UsuariosRol.IgnoreQueryFilters().CountAsync(i => i.ClubId == club.Id));

        await PagarPorPasarelaAsync(cobro);

        var reactivado = await ClubAsync(club);
        Assert.Equal(EstadoClub.ACTIVO, reactivado.Estado);
        Assert.Null(reactivado.MotivoSuspension);
        Assert.Equal(club.Nombre, reactivado.Nombre);
        Assert.Equal(
            integrantesAntes, await _fabrica.ConContextoAsync(c => c.UsuariosRol.IgnoreQueryFilters().CountAsync(i => i.ClubId == club.Id)));
    }

    [Fact]
    public async Task Un_club_suspendido_a_mano_sigue_suspendido_despues_de_un_pago()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "SUSPENDIDO" });
        var suspendido = await ClubAsync(club);
        Assert.Equal(MotivoSuspension.MANUAL, suspendido.MotivoSuspension);

        await PagarPorPasarelaAsync(cobro);

        var despues = await ClubAsync(club);
        Assert.Equal(EstadoClub.SUSPENDIDO, despues.Estado);
        Assert.Equal(MotivoSuspension.MANUAL, despues.MotivoSuspension);
    }

    [Fact]
    public async Task Suspender_a_mano_y_levantar_con_el_cobro_pagado_no_deja_motivo_de_impago()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await _fabrica.CobrosPlataforma.CrearCobroAsync(
            club, Noviembre, Noviembre.AddDays(5), estado: EstadoCobroPlataforma.PAGADO);

        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "SUSPENDIDO" });
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "ACTIVO" });

        var despues = await ClubAsync(club);
        Assert.Equal(EstadoClub.ACTIVO, despues.Estado);
        Assert.Null(despues.MotivoSuspension);
    }

    [Fact]
    public async Task Con_dos_cobros_vencidos_pagar_uno_no_reactiva_y_pagar_el_segundo_si()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var octubre = await CobroVencidoAsync(club, new DateOnly(2026, 10, 1));
        var noviembre = await CobroVencidoAsync(club);
        Fijar(20);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);

        await PagarPorPasarelaAsync(octubre);
        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);

        await PagarPorPasarelaAsync(noviembre);
        Assert.Equal(EstadoClub.ACTIVO, (await ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task Levantar_a_mano_una_suspension_por_impago_deja_el_club_activo_con_el_cobro_pendiente_y_pagable()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var cobro = await CobroVencidoAsync(club);
        Fijar(10);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);

        var levantar = await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "ACTIVO" });

        Assert.Equal(HttpStatusCode.OK, levantar.StatusCode);
        var activo = await ClubAsync(club);
        Assert.Equal(EstadoClub.ACTIVO, activo.Estado);
        Assert.Null(activo.MotivoSuspension);
        var guardado = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, guardado.Estado);
        Assert.True(guardado.SuspensionOmitida);

        // Un nuevo ciclo no lo vuelve a suspender y el cobro sigue pagable.
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.ACTIVO, (await ClubAsync(club)).Estado);
        var intento = await presidente.PostAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}/intentos");
        Assert.Equal(HttpStatusCode.Created, intento.StatusCode);

        // Un cobro posterior vencido sin pago sí lo suspende.
        await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 6));
        Fijar(8, mes: 12);
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.SUSPENDIDO, (await ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task Un_club_dado_de_baja_no_cambia_de_estado()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.DADO_DE_BAJA);
        await CobroVencidoAsync(club);
        Fijar(20);

        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        Assert.Equal(EstadoClub.DADO_DE_BAJA, (await ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task El_cambio_del_sistema_deja_constancia_y_no_conserva_al_autor_de_un_cambio_manual_anterior()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "SUSPENDIDO" });
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "ACTIVO" });
        Assert.NotNull((await ClubAsync(club)).EstadoCambiadoPorUsuarioId);
        await CobroVencidoAsync(club);
        Fijar(20);

        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();

        var despues = await ClubAsync(club);
        Assert.Equal(MotivoSuspension.IMPAGO, despues.MotivoSuspension);
        Assert.Null(despues.EstadoCambiadoPorUsuarioId);
    }
}
