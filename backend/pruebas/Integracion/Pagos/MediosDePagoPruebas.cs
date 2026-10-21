using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El PRESIDENTE publica por dónde se le paga al club y las familias lo ven en la cuenta de su jugador
/// (spec 015, historia 5; RF-026 a RF-028).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MediosDePagoPruebas
{
    private readonly FabricaApi _fabrica;

    public MediosDePagoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<ClienteDePrueba> ComoAsync(Usuario usuario) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

    private static object Medio(string entidad = "Nequi", string titular = "Club Valfor", string numero = "3001234567",
        string? nota = null, bool activo = true) => new { entidad, titular, numero, nota, activo };

    private async Task<Guid> CrearAsync(ClubDePagos c, object cuerpo)
    {
        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).PostAsync(c.Ruta("/medios-de-pago"), cuerpo);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("id").GetGuid();
    }

    private async Task<List<JsonElement>> ListaAsync(Usuario quien, ClubDePagos c)
    {
        var respuesta = await (await ComoAsync(quien)).GetAsync(c.Ruta("/medios-de-pago"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).EnumerateArray().ToList();
    }

    [Fact]
    public async Task El_presidente_crea_edita_desactiva_y_quita_un_medio()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);
        var id = await CrearAsync(c, Medio(nota: " Ahorros "));

        var creado = Assert.Single(await ListaAsync(c.Presidente.Usuario, c));
        Assert.Equal("Nequi", creado.GetProperty("entidad").GetString());
        Assert.Equal("Ahorros", creado.GetProperty("nota").GetString());

        var edicion = await presidente.PutAsync(
            c.Ruta($"/medios-de-pago/{id}"), Medio(entidad: "Bancolombia", numero: "123-456", activo: false));
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        var editado = Assert.Single(await ListaAsync(c.Presidente.Usuario, c));
        Assert.Equal("Bancolombia", editado.GetProperty("entidad").GetString());
        Assert.False(editado.GetProperty("activo").GetBoolean());

        var borrado = await presidente.DeleteAsync(c.Ruta($"/medios-de-pago/{id}"));
        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);
        Assert.Empty(await ListaAsync(c.Presidente.Usuario, c));
    }

    [Fact]
    public async Task La_familia_solo_ve_los_activos_y_el_presidente_y_el_directivo_ven_todos()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await CrearAsync(c, Medio("Nequi"));
        await CrearAsync(c, Medio("Bancolombia", activo: false));

        Assert.Equal(2, (await ListaAsync(c.Presidente.Usuario, c)).Count);
        Assert.Equal(2, (await ListaAsync(c.Directivo.Usuario, c)).Count);
        var familia = Assert.Single(await ListaAsync(c.Ana.Usuario, c));
        Assert.Equal("Nequi", familia.GetProperty("entidad").GetString());
    }

    [Fact]
    public async Task La_cuenta_del_jugador_trae_los_medios_activos_para_pagar()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await CrearAsync(c, Medio("Nequi"));
        await CrearAsync(c, Medio("Bancolombia", activo: false));

        var respuesta = await (await ComoAsync(c.Ana.Usuario)).GetAsync(c.Cuenta(c.Ana.Integrante));

        var medios = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("mediosPago").EnumerateArray().ToList();
        Assert.Equal("Nequi", Assert.Single(medios).GetProperty("entidad").GetString());
    }

    [Theory]
    [InlineData("", "Club", "300")]
    [InlineData("Nequi", "", "300")]
    [InlineData("Nequi", "Club", "")]
    public async Task Entidad_titular_y_numero_son_obligatorios(string entidad, string titular, string numero)
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await (await ComoAsync(c.Presidente.Usuario)).PostAsync(
            c.Ruta("/medios-de-pago"), Medio(entidad, titular, numero));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Empty(await ListaAsync(c.Presidente.Usuario, c));
    }

    [Fact]
    public async Task Los_textos_tienen_un_largo_maximo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var presidente = await ComoAsync(c.Presidente.Usuario);

        foreach (var cuerpo in new[]
        {
            Medio(entidad: new string('a', 41)),
            Medio(titular: new string('a', 101)),
            Medio(numero: new string('1', 61)),
            Medio(nota: new string('a', 201)),
        })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await presidente.PostAsync(c.Ruta("/medios-de-pago"), cuerpo)).StatusCode);
        }
    }

    [Fact]
    public async Task El_directivo_el_entrenador_y_la_familia_no_cambian_nada()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var id = await CrearAsync(c, Medio());

        foreach (var quien in new[] { c.Directivo.Usuario, c.Entrenador.Usuario, c.Ana.Usuario })
        {
            var cliente = await ComoAsync(quien);

            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync(c.Ruta("/medios-de-pago"), Medio("Otro"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsync(c.Ruta($"/medios-de-pago/{id}"), Medio("Otro"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync(c.Ruta($"/medios-de-pago/{id}"))).StatusCode);
        }

        Assert.Equal("Nequi", Assert.Single(await ListaAsync(c.Presidente.Usuario, c)).GetProperty("entidad").GetString());
    }

    [Fact]
    public async Task El_entrenador_no_ve_los_medios_de_pago()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var respuesta = await (await ComoAsync(c.Entrenador.Usuario)).GetAsync(c.Ruta("/medios-de-pago"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Los_medios_de_otro_club_no_se_ven_ni_se_tocan()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var ajeno = await _fabrica.Pagos.CrearClubAsync();
        var idAjeno = await CrearAsync(ajeno, Medio("Daviplata"));
        var presidente = await ComoAsync(c.Presidente.Usuario);

        Assert.Empty(await ListaAsync(c.Presidente.Usuario, c));
        var edicion = await presidente.PutAsync(c.Ruta($"/medios-de-pago/{idAjeno}"), Medio("Otro"));
        var borrado = await presidente.DeleteAsync(c.Ruta($"/medios-de-pago/{idAjeno}"));
        var delAjeno = await presidente.GetAsync(ajeno.Ruta("/medios-de-pago"));

        Assert.Equal(HttpStatusCode.NotFound, edicion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, borrado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delAjeno.StatusCode);
        Assert.Equal("Daviplata", Assert.Single(await ListaAsync(ajeno.Presidente.Usuario, ajeno)).GetProperty("entidad").GetString());
    }

    [Fact]
    public async Task Quitar_un_medio_no_cambia_los_pagos_hechos_con_esa_entidad()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var id = await CrearAsync(c, Medio("Nequi"));
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        await _fabrica.ConContextoAsync(contexto => contexto.Pagos.IgnoreQueryFilters().Where(fila => fila.Id == pago.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Entidad, "Nequi")));

        await (await ComoAsync(c.Presidente.Usuario)).DeleteAsync(c.Ruta($"/medios-de-pago/{id}"));

        var conservado = await _fabrica.ConContextoAsync(contexto =>
            contexto.Pagos.IgnoreQueryFilters().SingleAsync(fila => fila.Id == pago.Id));
        Assert.Equal("Nequi", conservado.Entidad);
        Assert.Equal(65000m, conservado.Valor);
    }
}
