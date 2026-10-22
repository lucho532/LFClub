using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Base.SembradorCobrosPlataforma;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Lo que el DESARROLLADOR ve y hace con los cobros de un club (spec 017, historia 5).</summary>
[Collection(ColeccionApi.Nombre)]
public class CobrosDeUnClubPruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public CobrosDeUnClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static string Ruta(Club club) => $"/api/plataforma/clubes/{club.Id}/cobros";

    [Fact]
    public async Task El_desarrollador_lista_los_cobros_de_un_club_sin_datos_personales()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, presidente) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5), 45000);
        await _fabrica.CobrosPlataforma.CrearCobroAsync(club, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 6), 40000, EstadoCobroPlataforma.PAGADO);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.GetAsync(Ruta(club));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var cobros = JsonSerializer.Deserialize<JsonElement>(texto);
        Assert.Equal(["2026-11", "2026-10"], cobros.EnumerateArray().Select(c => c.GetProperty("periodo").GetString()).ToList());
        Assert.DoesNotContain(presidente.Nombres, texto);
        Assert.DoesNotContain("correo", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Listar_los_cobros_de_un_club_inexistente_responde_404()
    {
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.GetAsync($"/api/plataforma/clubes/{Guid.NewGuid()}/cobros")).StatusCode);
    }

    [Fact]
    public async Task El_pago_manual_crea_el_pago_con_origen_manual_marca_el_cobro_y_reactiva_el_club_suspendido_por_impago()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        _fabrica.Reloj.UsarElReal();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 20), new TimeOnly(10, 0));
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(MotivoSuspension.IMPAGO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).MotivoSuspension);

        var respuesta = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/pago-manual");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("PAGADO", dto.GetProperty("estado").GetString());
        Assert.Equal("MANUAL", dto.GetProperty("origenDelPago").GetString());
        var pago = await _fabrica.ConContextoAsync(c => c.PagosPlataforma.AsNoTracking().SingleAsync(p => p.CobroId == cobro.Id));
        var autor = await _fabrica.ConContextoAsync(c => c.Usuarios.SingleAsync(u => u.EsDesarrollador));
        Assert.Equal(OrigenPagoPlataforma.MANUAL, pago.Origen);
        Assert.Equal(autor.Id, pago.RegistradoPorUsuarioId);
        Assert.Equal(cobro.Total, pago.Valor);
        Assert.Equal(EstadoClub.ACTIVO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task El_pago_manual_no_levanta_una_suspension_manual()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{club.Id}/estado", new { estado = "SUSPENDIDO" });

        await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/pago-manual");

        Assert.Equal(EstadoClub.SUSPENDIDO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);
    }

    [Theory]
    [InlineData(EstadoCobroPlataforma.PAGADO)]
    [InlineData(EstadoCobroPlataforma.ANULADO)]
    public async Task Marcar_o_anular_un_cobro_ya_pagado_o_anulado_responde_409(EstadoCobroPlataforma estado)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5), estado: estado);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var marcar = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/pago-manual");
        var anular = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/anulacion", new { motivo = "Cobro repetido" });

        Assert.Equal(HttpStatusCode.Conflict, marcar.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, anular.StatusCode);
        Assert.Equal("cobro_no_pendiente", await ClienteDePrueba.CodigoAsync(marcar));
    }

    [Fact]
    public async Task Un_cobro_de_otro_club_responde_404_al_marcarlo_o_anularlo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otro = await _fabrica.Sembrador.CrearClubAsync();
        var cobroAjeno = await _fabrica.CobrosPlataforma.CrearCobroAsync(otro, Noviembre, Noviembre.AddDays(5));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.PostAsync($"{Ruta(club)}/{cobroAjeno.Id}/pago-manual")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await desarrollador.PostAsync($"{Ruta(club)}/{cobroAjeno.Id}/anulacion", new { motivo = "Prueba" })).StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await _fabrica.CobrosPlataforma.CobrosDeAsync(otro)).Single().Estado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task Anular_exige_un_motivo_de_3_a_300_caracteres(string motivo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var corto = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/anulacion", new { motivo });
        var largo = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/anulacion", new { motivo = new string('x', 301) });

        Assert.Equal(HttpStatusCode.BadRequest, corto.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, largo.StatusCode);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, (await _fabrica.CobrosPlataforma.CobrosDeAsync(club)).Single().Estado);
    }

    [Fact]
    public async Task Un_cobro_anulado_deja_constancia_no_se_puede_pagar_y_no_afecta_la_suspension()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/anulacion", new { motivo = "  Club en periodo de prueba  " });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        var autor = await _fabrica.ConContextoAsync(c => c.Usuarios.SingleAsync(u => u.EsDesarrollador));
        Assert.Equal(EstadoCobroPlataforma.ANULADO, guardado.Estado);
        Assert.Equal("Club en periodo de prueba", guardado.MotivoAnulacion);
        Assert.Equal(autor.Id, guardado.AnuladoPorUsuarioId);
        Assert.NotNull(guardado.AnuladoEn);
        var intento = await presidente.PostAsync($"/api/clubes/{club.Id}/pago-plataforma/cobros/{cobro.Id}/intentos");
        Assert.Equal(HttpStatusCode.Conflict, intento.StatusCode);
        Assert.Equal(EstadoClub.ACTIVO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);
    }

    [Fact]
    public async Task Anular_el_cobro_vencido_reactiva_un_club_suspendido_por_impago()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cobro = await _fabrica.CobrosPlataforma.CrearCobroAsync(club, Noviembre, Noviembre.AddDays(5));
        _fabrica.Reloj.UsarElReal();
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 20), new TimeOnly(10, 0));
        await _fabrica.CobrosPlataforma.AplicarSuspensionAsync();
        Assert.Equal(EstadoClub.SUSPENDIDO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);

        await desarrollador.PostAsync($"{Ruta(club)}/{cobro.Id}/anulacion", new { motivo = "Cobro por error" });

        Assert.Equal(EstadoClub.ACTIVO, (await _fabrica.CobrosPlataforma.ClubAsync(club)).Estado);
    }

}
