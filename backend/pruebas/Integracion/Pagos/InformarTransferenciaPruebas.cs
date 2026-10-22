using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// La familia informa una transferencia con su soporte: queda en revisión sin tocar saldos, recibos ni
/// recaudo (spec 015, historia 6; RF-021; clarificaciones 3 y 4).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class InformarTransferenciaPruebas
{
    private readonly FabricaApi _fabrica;

    public InformarTransferenciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private static string Ruta(ClubDePagos c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/pagos/informados");

    private Dictionary<string, string> Campos(string valor = "65000", string entidad = "Nequi") => new()
    {
        ["valor"] = valor,
        ["entidad"] = entidad,
        ["referencia"] = "REF-1",
        ["fecha"] = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd"),
    };

    private async Task<(ClubDePagos Club, Cargo Cargo)> ClubConUnCargoAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        return (c, cargo);
    }

    private Task<int> PagosAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Pagos.IgnoreQueryFilters().CountAsync(pago => pago.UsuarioRolId == usuarioRolId));

    [Fact]
    public async Task Informar_con_imagen_deja_el_pago_en_revision_sin_aplicaciones_ni_recibo()
    {
        var (c, cargo) = await ClubConUnCargoAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        var respuesta = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var pago = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("EN_REVISION", pago.GetProperty("estado").GetString());
        Assert.Equal("TRANSFERENCIA", pago.GetProperty("medio").GetString());
        Assert.Equal("Nequi", pago.GetProperty("entidad").GetString());
        Assert.Equal(JsonValueKind.Null, pago.GetProperty("numeroRecibo").ValueKind);
        Assert.True(pago.GetProperty("tieneSoporte").GetBoolean());
        Assert.False(pago.GetProperty("tieneResguardo").GetBoolean());
        Assert.Equal(0m, await _fabrica.Pagos.PagadoAsync(cargo));
        var aplicaciones = await _fabrica.ConContextoAsync(contexto =>
            contexto.AplicacionesPago.IgnoreQueryFilters().CountAsync(a => a.ClubId == c.Club.Id));
        Assert.Equal(0, aplicaciones);
    }

    [Fact]
    public async Task Informar_no_cambia_el_saldo_ni_el_estado_de_cuenta()
    {
        var (c, _) = await ClubConUnCargoAsync();
        await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());

        var cuenta = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Cuenta(c.Ana.Integrante)));

        Assert.Equal(65000m, cuenta.GetProperty("saldo").GetDecimal());
        Assert.Equal("DEBE", cuenta.GetProperty("estado").GetString());
        Assert.Equal("EN_REVISION", Assert.Single(cuenta.GetProperty("pagos").EnumerateArray()).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Pueden_estar_varios_en_revision_a_la_vez()
    {
        var (c, _) = await ClubConUnCargoAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        var uno = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos("30000"), Imagenes.Png());
        var dos = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos("65000"), Imagenes.Jpeg());

        Assert.Equal(HttpStatusCode.Created, uno.StatusCode);
        Assert.Equal(HttpStatusCode.Created, dos.StatusCode);
        Assert.Equal(2, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Sin_imagen_o_con_un_archivo_que_no_es_imagen_responde_400_y_no_queda_nada()
    {
        var (c, _) = await ClubConUnCargoAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        var sinImagen = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos());
        var pdf = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), "%PDF-1.4 contenido"u8.ToArray(), "x.pdf");
        var texto = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Texto());

        Assert.Equal("soporte_requerido", await ClienteDePrueba.CodigoAsync(sinImagen));
        Assert.Equal("soporte_invalido", await ClienteDePrueba.CodigoAsync(pdf));
        Assert.Equal("soporte_invalido", await ClienteDePrueba.CodigoAsync(texto));
        Assert.All(new[] { sinImagen, pdf, texto }, respuesta => Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode));
        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Una_imagen_de_mas_de_5_MB_responde_400_soporte_demasiado_grande()
    {
        var (c, _) = await ClubConUnCargoAsync();
        var ana = await ComoAsync(c.Ana.Usuario);

        var respuesta = await ana.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png((5 * 1024 * 1024) - 7));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("soporte_demasiado_grande", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5000")]
    [InlineData("65000.5")]
    [InlineData("abc")]
    public async Task Un_valor_invalido_responde_400(string valor)
    {
        var (c, _) = await ClubConUnCargoAsync();

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(
            Ruta(c, c.Ana.Integrante), Campos(valor), Imagenes.Png());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Sin_entidad_responde_400()
    {
        var (c, _) = await ClubConUnCargoAsync();

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(
            Ruta(c, c.Ana.Integrante), Campos(entidad: ""), Imagenes.Png());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_valor_mayor_que_lo_pendiente_responde_409()
    {
        var (c, _) = await ClubConUnCargoAsync();

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(
            Ruta(c, c.Ana.Integrante), Campos("70000"), Imagenes.Png());

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("pago_excede_deuda", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Solo_se_informa_sobre_el_propio_jugador()
    {
        var (c, _) = await ClubConUnCargoAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(ajeno.Ana.Integrante, new DateOnly(2026, 10, 1));
        var beto = await ComoAsync(c.Beto.Usuario);

        var deAna = await beto.PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());
        var deOtroClub = await beto.PostFormularioAsync(Ruta(c, ajeno.Ana.Integrante), Campos(), Imagenes.Png());

        Assert.Equal(HttpStatusCode.NotFound, deAna.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deOtroClub.StatusCode);
        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task El_presidente_el_directivo_y_el_entrenador_no_usan_esta_operacion()
    {
        var (c, _) = await ClubConUnCargoAsync();

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario, c.Entrenador.Usuario })
        {
            var respuesta = await (await ComoAsync(quien)).PostFormularioAsync(
                Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        Assert.Equal(0, await PagosAsync(c.Ana.Integrante.Id));
    }

    [Fact]
    public async Task Un_jugador_sin_nada_pendiente_responde_409_sin_deuda()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(
            Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("sin_deuda", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Un_pago_en_revision_no_cuenta_en_el_recaudo_ni_en_el_estado_de_la_lista()
    {
        var (c, _) = await ClubConUnCargoAsync();
        await (await ComoAsync(c.Ana.Usuario)).PostFormularioAsync(Ruta(c, c.Ana.Integrante), Campos(), Imagenes.Png());

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(
            await (await ComoAsync(c.Presidente.Usuario)).GetAsync(c.Ruta("/jugadores")));

        var ana = lista.EnumerateArray().SelectMany(g => g.GetProperty("jugadores").EnumerateArray())
            .Single(j => j.GetProperty("usuarioRolId").GetGuid() == c.Ana.Integrante.Id);
        Assert.Equal("DEBE", ana.GetProperty("estadoCuenta").GetString());
        Assert.Equal(EstadoPago.EN_REVISION, (await _fabrica.ConContextoAsync(contexto =>
            contexto.Pagos.IgnoreQueryFilters().SingleAsync(p => p.UsuarioRolId == c.Ana.Integrante.Id))).Estado);
    }
}
