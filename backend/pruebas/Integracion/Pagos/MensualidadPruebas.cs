using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El PRESIDENTE define el valor de la mensualidad del club y el propio de cada jugador; solo cambia los
/// cargos futuros (spec 015, historia 2; RF-011 a RF-014; CE-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MensualidadPruebas
{
    private readonly FabricaApi _fabrica;

    public MensualidadPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(LFClub.Dominio.Entidades.Usuario usuario) =>
        _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    [Fact]
    public async Task El_presidente_y_el_directivo_consultan_el_valor_y_los_valores_propios()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        await _fabrica.Pagos.FijarValorPropioAsync(c.Beto.Integrante, 50000m);

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta("/mensualidad"));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var cuerpo = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
            Assert.Equal(65000m, cuerpo.GetProperty("valorClub").GetDecimal());
            var propio = Assert.Single(cuerpo.GetProperty("jugadoresConValorPropio").EnumerateArray());
            Assert.Equal(c.Beto.Integrante.Id, propio.GetProperty("usuarioRolId").GetGuid());
            Assert.Equal(50000m, propio.GetProperty("valor").GetDecimal());
        }
    }

    [Fact]
    public async Task Un_club_sin_valor_lo_dice_con_nulo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(valor: null);

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/mensualidad"));

        var cuerpo = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(JsonValueKind.Null, cuerpo.GetProperty("valorClub").ValueKind);
        Assert.Empty(cuerpo.GetProperty("jugadoresConValorPropio").EnumerateArray());
    }

    [Fact]
    public async Task Cambiar_el_valor_no_toca_los_cargos_ya_generados_y_los_siguientes_nacen_con_el_nuevo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        // Las sesiones se abren antes de fijar el reloj: se emiten con él y se validan con el real.
        var presidente = await ComoAsync(c.Presidente.Usuario);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 15), new TimeOnly(10, 0));
        try
        {
            await _fabrica.Pagos.GenerarAsync();

            var respuesta = await presidente.PutAsync(c.Ruta("/mensualidad"), new { valor = 70000 });

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(
                70000m,
                (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("valorClub").GetDecimal());
            Assert.All(await _fabrica.Pagos.CargosDelClubAsync(c.Club), cargo => Assert.Equal(65000m, cargo.Valor));

            _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 12, 1), new TimeOnly(0, 5));
            await _fabrica.Pagos.GenerarAsync();
            var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
            Assert.Equal(4, cargos.Count);
            Assert.All(
                cargos.Where(cargo => cargo.Mes == new DateOnly(2026, 12, 1)),
                cargo => Assert.Equal(70000m, cargo.Valor));
        }
        finally
        {
            _fabrica.Reloj.UsarElReal();
        }
    }

    [Fact]
    public async Task Fijar_el_valor_a_un_club_sin_valor_genera_en_el_acto_los_cargos_del_mes_en_curso()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(valor: null);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        Assert.Empty(await _fabrica.Pagos.CargosDelClubAsync(c.Club));

        var respuesta = await presidente.PutAsync(c.Ruta("/mensualidad"), new { valor = 65000 });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
        Assert.Equal(2, cargos.Count);
        Assert.All(cargos, cargo => Assert.Equal(65000m, cargo.Valor));
        Assert.All(cargos, cargo => Assert.Equal(_fabrica.Pagos.PrimerDiaDelMes(), cargo.Mes));
    }

    [Fact]
    public async Task El_presidente_fija_un_valor_propio_a_varios_jugadores_y_solo_a_ellos()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        var otro = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.Categoria, "Caro");
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PutAsync(
            c.Ruta("/mensualidad/jugadores"),
            new { usuarioRolIds = new[] { c.Ana.Integrante.Id, c.Beto.Integrante.Id }, valor = 50000 });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var propios = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta))
            .GetProperty("jugadoresConValorPropio").EnumerateArray()
            .Select(fila => fila.GetProperty("usuarioRolId").GetGuid()).Order().ToList();
        Assert.Equal(new[] { c.Ana.Integrante.Id, c.Beto.Integrante.Id }.Order(), propios);
        Assert.DoesNotContain(otro.Integrante.Id, propios);
    }

    [Fact]
    public async Task Quitar_el_valor_propio_devuelve_al_jugador_al_valor_del_club()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        await _fabrica.Pagos.FijarValorPropioAsync(c.Ana.Integrante, 50000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PutAsync(
            c.Ruta("/mensualidad/jugadores"), new { usuarioRolIds = new[] { c.Ana.Integrante.Id }, valor = (int?)null });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta))
            .GetProperty("jugadoresConValorPropio").EnumerateArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-65000)]
    [InlineData(65000.5)]
    public async Task Un_valor_vacio_cero_negativo_o_con_centavos_responde_400_y_no_guarda_nada(double valor)
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var delClub = await presidente.PutAsync(c.Ruta("/mensualidad"), new { valor });
        var propio = await presidente.PutAsync(
            c.Ruta("/mensualidad/jugadores"), new { usuarioRolIds = new[] { c.Ana.Integrante.Id }, valor });

        foreach (var respuesta in new[] { delClub, propio })
        {
            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        var guardado = await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/mensualidad"));
        var cuerpo = await ClienteDePrueba.LeerAsync<JsonElement>(guardado);
        Assert.Equal(65000m, cuerpo.GetProperty("valorClub").GetDecimal());
        Assert.Empty(cuerpo.GetProperty("jugadoresConValorPropio").EnumerateArray());
    }

    [Fact]
    public async Task Sin_valor_en_el_cuerpo_o_sin_jugadores_responde_400()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        Assert.Equal(HttpStatusCode.BadRequest, (await presidente.PutAsync(c.Ruta("/mensualidad"), new { })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await presidente.PutAsync(c.Ruta("/mensualidad/jugadores"), new { usuarioRolIds = Array.Empty<Guid>(), valor = 1000 }))
            .StatusCode);
    }

    [Fact]
    public async Task Solo_el_presidente_cambia_el_valor_el_directivo_el_entrenador_y_la_familia_reciben_403()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);

        foreach (var quien in new[] { c.Directivo.Usuario, c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var cliente = await ComoAsync(quien);

            var delClub = await cliente.PutAsync(c.Ruta("/mensualidad"), new { valor = 1000 });
            var propio = await cliente.PutAsync(
                c.Ruta("/mensualidad/jugadores"), new { usuarioRolIds = new[] { c.Ana.Integrante.Id }, valor = 1000 });

            Assert.Equal(HttpStatusCode.Forbidden, delClub.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, propio.StatusCode);
        }

        var ver = await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/mensualidad"));
        Assert.Equal(65000m, (await ClienteDePrueba.LeerAsync<JsonElement>(ver)).GetProperty("valorClub").GetDecimal());
    }

    [Fact]
    public async Task El_entrenador_y_la_familia_no_consultan_la_mensualidad()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);

        foreach (var quien in new[] { c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta("/mensualidad"));

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }
    }

    [Fact]
    public async Task Un_jugador_de_otro_club_responde_404_y_no_cambia_ninguno()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        var ajeno = await _fabrica.Pagos.CrearClubAsync(65000m);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PutAsync(
            c.Ruta("/mensualidad/jugadores"),
            new { usuarioRolIds = new[] { c.Ana.Integrante.Id, ajeno.Ana.Integrante.Id }, valor = 50000 });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        var propios = await presidente.GetAsync(c.Ruta("/mensualidad"));
        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(propios))
            .GetProperty("jugadoresConValorPropio").EnumerateArray());
        var delAjeno = await (await ComoAsync(ajeno.Presidente.Usuario)).GetAsync(ajeno.Ruta("/mensualidad"));
        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(delAjeno))
            .GetProperty("jugadoresConValorPropio").EnumerateArray());
    }
}
