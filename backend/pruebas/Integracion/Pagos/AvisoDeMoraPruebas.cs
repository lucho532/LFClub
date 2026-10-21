using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El aviso fijo de mora del inicio de la familia: los tres estados según el día en Colombia, cómo lo
/// mueven los pagos del PRESIDENTE y quién lo ve (spec 016; RF-001 a RF-017; CE-001 a CE-007).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisoDeMoraPruebas : IDisposable
{
    private static readonly DateOnly Octubre = new(2026, 10, 1);
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public AvisoDeMoraPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        EnColombia(new DateOnly(2026, 11, 20), new TimeOnly(10, 0));
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private (DateOnly Dia, TimeOnly Hora) _momento;

    private void EnColombia(DateOnly dia, TimeOnly hora)
    {
        _momento = (dia, hora);
        _fabrica.Reloj.FijarEnColombia(dia, hora);
    }

    /// <summary>Las sesiones se emiten con el reloj real y se validan con él: se abren antes de fijar el día.</summary>
    private async Task<ClienteDePrueba> ComoAsync(Usuario usuario)
    {
        _fabrica.Reloj.UsarElReal();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        _fabrica.Reloj.FijarEnColombia(_momento.Dia, _momento.Hora);
        return cliente;
    }

    private static string Ruta(ClubDePagos c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/cuenta/aviso");

    private async Task<JsonElement> AvisoAsync(ClubDePagos c, (Usuario Usuario, UsuarioRol Integrante) jugador)
    {
        var respuesta = await (await ComoAsync(jugador.Usuario)).GetAsync(Ruta(c, jugador.Integrante));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }

    private async Task<string?> EstadoAsync(ClubDePagos c, (Usuario Usuario, UsuarioRol Integrante) jugador) =>
        (await AvisoAsync(c, jugador)).GetProperty("estado").GetString();

    // Historia 4: qué cuenta como estar en mora

    [Fact]
    public async Task Sin_cargos_el_aviso_es_verde()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var aviso = await AvisoAsync(c, c.Ana);

        Assert.Equal("AL_DIA", aviso.GetProperty("estado").GetString());
        Assert.Equal(0m, aviso.GetProperty("valorVencido").GetDecimal());
        Assert.Equal(0, aviso.GetProperty("mesesVencidos").GetInt32());
        Assert.Equal(JsonValueKind.Null, aviso.GetProperty("venceEl").ValueKind);
        Assert.Equal(0, aviso.GetProperty("pagosEnRevision").GetInt32());
    }

    [Fact]
    public async Task El_dia_5_la_mensualidad_del_mes_es_ambar_y_el_dia_6_pasa_a_rojo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Noviembre);

        EnColombia(new DateOnly(2026, 11, 5), new TimeOnly(23, 59));
        var enPlazo = await AvisoAsync(c, c.Ana);
        EnColombia(new DateOnly(2026, 11, 6), new TimeOnly(0, 0));
        var vencida = await AvisoAsync(c, c.Ana);

        Assert.Equal("PENDIENTE_EN_PLAZO", enPlazo.GetProperty("estado").GetString());
        Assert.Equal(65000m, enPlazo.GetProperty("valorEnPlazo").GetDecimal());
        Assert.Equal("2026-11-05", enPlazo.GetProperty("venceEl").GetString());
        Assert.Equal("EN_MORA", vencida.GetProperty("estado").GetString());
        Assert.Equal(65000m, vencida.GetProperty("valorVencido").GetDecimal());
        Assert.Equal(1, vencida.GetProperty("mesesVencidos").GetInt32());
    }

    [Fact]
    public async Task Varias_mensualidades_vencidas_dan_un_solo_aviso_con_el_total_y_un_cargo_OTRO_no_cuenta()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre, 70000m);
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Noviembre);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            contexto.Cargos.Add(new Cargo
            {
                ClubId = c.Club.Id,
                UsuarioRolId = c.Ana.Integrante.Id,
                Tipo = TipoCargo.OTRO,
                Valor = 30000m,
                CreadoEn = DateTime.UtcNow,
            });
            await contexto.SaveChangesAsync();
            return 0;
        });

        var aviso = await AvisoAsync(c, c.Ana);

        Assert.Equal("EN_MORA", aviso.GetProperty("estado").GetString());
        Assert.Equal(135000m, aviso.GetProperty("valorVencido").GetDecimal());
        Assert.Equal(2, aviso.GetProperty("mesesVencidos").GetInt32());
    }

    [Fact]
    public async Task Cada_jugador_tiene_su_propio_aviso()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);

        Assert.Equal("EN_MORA", await EstadoAsync(c, c.Ana));
        Assert.Equal("AL_DIA", await EstadoAsync(c, c.Beto));
    }

    // Historia 2: los pagos mueven el color

    [Fact]
    public async Task Un_pago_parcial_sigue_en_rojo_con_lo_que_falta_y_el_que_cubre_todo_pasa_a_verde()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 25000m, cargo);

        var parcial = await AvisoAsync(c, c.Ana);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 40000m, cargo);

        Assert.Equal("EN_MORA", parcial.GetProperty("estado").GetString());
        Assert.Equal(40000m, parcial.GetProperty("valorVencido").GetDecimal());
        Assert.Equal("AL_DIA", await EstadoAsync(c, c.Ana));
    }

    [Fact]
    public async Task Pagar_lo_vencido_deja_ambar_si_queda_la_mensualidad_del_mes_dentro_del_plazo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var octubre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Noviembre);
        EnColombia(new DateOnly(2026, 11, 3), new TimeOnly(10, 0));

        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, octubre);

        Assert.Equal("PENDIENTE_EN_PLAZO", await EstadoAsync(c, c.Ana));
    }

    [Fact]
    public async Task Un_pago_en_revision_no_cambia_el_estado_pero_se_cuenta_y_al_confirmarlo_pasa_a_verde()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);

        var antes = await AvisoAsync(c, c.Ana);
        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/confirmacion"));
        var despues = await AvisoAsync(c, c.Ana);

        Assert.Equal("EN_MORA", antes.GetProperty("estado").GetString());
        Assert.Equal(1, antes.GetProperty("pagosEnRevision").GetInt32());
        Assert.Equal("AL_DIA", despues.GetProperty("estado").GetString());
        Assert.Equal(0, despues.GetProperty("pagosEnRevision").GetInt32());
    }

    [Fact]
    public async Task Un_pago_rechazado_no_cambia_el_estado()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        var informado = await _fabrica.Pagos.CrearPagoEnRevisionAsync(c.Club, c.Ana.Integrante, 65000m, conSoporte: true);

        var presidente = await ComoAsync(c.Presidente.Usuario);
        await presidente.PostAsync(c.Ruta($"/pagos/{informado.Id}/rechazo"), new { motivo = "La imagen no se ve" });

        var aviso = await AvisoAsync(c, c.Ana);
        Assert.Equal("EN_MORA", aviso.GetProperty("estado").GetString());
        Assert.Equal(0, aviso.GetProperty("pagosEnRevision").GetInt32());
    }

    [Fact]
    public async Task Anular_el_pago_que_dejaba_al_dia_devuelve_el_aviso_a_rojo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, Octubre);
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        Assert.Equal("AL_DIA", await EstadoAsync(c, c.Ana));

        var presidente = await ComoAsync(c.Presidente.Usuario);
        var anulacion = await presidente.PostAsync(c.Ruta($"/pagos/{pago.Id}/anulacion"), new { motivo = "Error de jugador" });

        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        Assert.Equal("EN_MORA", await EstadoAsync(c, c.Ana));
    }
}
