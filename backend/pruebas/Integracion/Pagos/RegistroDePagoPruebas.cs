using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El PRESIDENTE registra un pago recibido directamente: nace confirmado, se aplica a los cargos, el
/// jugador queda al día y el pago tiene recibo (spec 015, historia 4; RF-017 a RF-025, RF-035).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RegistroDePagoPruebas
{
    private readonly FabricaApi _fabrica;

    public RegistroDePagoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private static string RutaPagos(ClubDePagos c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/pagos");

    private string Hoy => _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd");

    private object Cuerpo(
        decimal valor = 65000m, string medio = "EFECTIVO", string? entidad = null, string? referencia = null,
        Guid[]? cargoIds = null, string? fecha = null) =>
        new { valor, medio, entidad, referencia, fecha = fecha ?? Hoy, cargoIds };

    private async Task<(ClubDePagos Club, Cargo Septiembre, Cargo Octubre)> ClubConDosCargosAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var septiembre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 9, 1));
        var octubre = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        return (c, septiembre, octubre);
    }

    private async Task<JsonElement> CuentaAsync(ClubDePagos c, UsuarioRol jugador) =>
        await ClienteDePrueba.LeerAsync<JsonElement>(await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Cuenta(jugador)));

    [Fact]
    public async Task Un_pago_en_efectivo_por_lo_que_se_debe_deja_al_jugador_al_dia_con_su_recibo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("CONFIRMADO", pago.GetProperty("estado").GetString());
        Assert.Equal(65000m, pago.GetProperty("valor").GetDecimal());
        Assert.Equal("EFECTIVO", pago.GetProperty("medio").GetString());
        Assert.Equal(1, pago.GetProperty("numeroRecibo").GetInt32());
        Assert.Equal(0m, pago.GetProperty("saldoTrasPago").GetDecimal());
        Assert.Equal("Mensualidad octubre de 2026", pago.GetProperty("concepto").GetString());
        Assert.Equal($"{c.Presidente.Integrante.Nombres} {c.Presidente.Integrante.Apellidos}", pago.GetProperty("confirmadoPorNombre").GetString());
        Assert.True(pago.GetProperty("tieneResguardo").GetBoolean());
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
        var cuenta = await CuentaAsync(c, c.Ana.Integrante);
        Assert.Equal("AL_DIA", cuenta.GetProperty("estado").GetString());
        Assert.Equal(0m, cuenta.GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Una_transferencia_lleva_su_entidad_y_su_referencia_y_el_efectivo_no_pide_entidad()
    {
        var (c, _, _) = await ClubConDosCargosAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var transferencia = await presidente.PostAsync(
            RutaPagos(c, c.Ana.Integrante), Cuerpo(medio: "TRANSFERENCIA", entidad: "Nequi", referencia: "ABC123"));
        var efectivo = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo(entidad: "Nequi"));

        var pagoTransferencia = await ClienteDePrueba.LeerAsync<JsonElement>(transferencia);
        Assert.Equal("Nequi", pagoTransferencia.GetProperty("entidad").GetString());
        Assert.Equal("ABC123", pagoTransferencia.GetProperty("referencia").GetString());
        Assert.Equal(HttpStatusCode.Created, efectivo.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ClienteDePrueba.LeerAsync<JsonElement>(efectivo)).GetProperty("entidad").ValueKind);
    }

    [Fact]
    public async Task Sin_cargos_elegidos_cubre_primero_el_mas_antiguo_y_un_abono_parcial_deja_pago_parcial()
    {
        var (c, septiembre, octubre) = await ClubConDosCargosAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo(valor: 90000m));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(septiembre));
        Assert.Equal(25000m, await _fabrica.Pagos.PagadoAsync(octubre));
        var cuenta = await CuentaAsync(c, c.Ana.Integrante);
        Assert.Equal("PAGO_PARCIAL", cuenta.GetProperty("estado").GetString());
        Assert.Equal(40000m, cuenta.GetProperty("saldo").GetDecimal());
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(40000m, pago.GetProperty("saldoTrasPago").GetDecimal());
        Assert.Equal("Mensualidad septiembre de 2026; Mensualidad octubre de 2026", pago.GetProperty("concepto").GetString());
    }

    [Fact]
    public async Task Con_cargos_elegidos_cubre_solo_esos()
    {
        var (c, septiembre, octubre) = await ClubConDosCargosAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(
            RutaPagos(c, c.Ana.Integrante), Cuerpo(cargoIds: [octubre.Id]));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(septiembre));
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(octubre));
    }

    [Fact]
    public async Task Un_valor_mayor_que_lo_pendiente_responde_409_con_lo_que_se_debe_y_no_guarda_nada()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo(valor: 70000m));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("pago_excede_deuda", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(65000m, (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("debe").GetDecimal());
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(cargo));
        Assert.Empty((await CuentaAsync(c, c.Ana.Integrante)).GetProperty("pagos").EnumerateArray());
    }

    [Fact]
    public async Task Un_jugador_sin_nada_pendiente_responde_409_sin_deuda()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo());

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("sin_deuda", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-65000)]
    [InlineData(65000.5)]
    public async Task Cero_negativo_o_con_centavos_responde_400(double valor)
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), new { valor, medio = "EFECTIVO", fecha = Hoy });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Una_transferencia_sin_entidad_o_con_fecha_futura_responde_400()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var sinEntidad = await presidente.PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo(medio: "TRANSFERENCIA"));
        var futura = await presidente.PostAsync(
            RutaPagos(c, c.Ana.Integrante), Cuerpo(fecha: _fabrica.Reloj.Hoy.AddDays(2).ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, sinEntidad.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, futura.StatusCode);
    }

    [Fact]
    public async Task El_directivo_el_entrenador_y_la_familia_reciben_403_y_no_queda_ningun_pago()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));

        foreach (var quien in new[] { c.Directivo.Usuario, c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).PostAsync(RutaPagos(c, c.Ana.Integrante), Cuerpo());

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        Assert.Empty((await CuentaAsync(c, c.Ana.Integrante)).GetProperty("pagos").EnumerateArray());
    }

    [Fact]
    public async Task Se_puede_registrar_un_pago_de_un_jugador_retirado_que_debe()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Beto.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Categorias.RetirarAsync(c.Beto.Integrante, c.Presidente.Integrante);
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var respuesta = await presidente.PostAsync(RutaPagos(c, c.Beto.Integrante), Cuerpo());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Un_jugador_de_otro_club_o_que_no_existe_responde_404()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(ajeno.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        var deOtroClub = await presidente.PostAsync(RutaPagos(c, ajeno.Ana.Integrante), Cuerpo());
        var inexistente = await presidente.PostAsync(RutaPagos(c, new UsuarioRol { Id = Guid.NewGuid() }), Cuerpo());
        var noEsJugador = await presidente.PostAsync(RutaPagos(c, c.Directivo.Integrante), Cuerpo());

        foreach (var respuesta in new[] { deOtroClub, inexistente, noEsJugador })
        {
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }

        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(
            (await _fabrica.Pagos.CargosDelClubAsync(ajeno.Club)).Single()));
    }
}
