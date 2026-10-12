using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LaPecosa.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// Un doble toque no publica dos comunicados: la clave hace idempotente la publicación (spec 011,
/// RF-010; research §6).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class IdempotenciaPruebas
{
    private readonly FabricaApi _fabrica;

    public IdempotenciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<int> ContarAsync(Guid clubId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Comunicados.IgnoreQueryFilters().CountAsync(comunicado => comunicado.ClubId == clubId));

    [Fact]
    public async Task La_misma_clave_dos_veces_devuelve_el_mismo_comunicado_con_200()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var cuerpo = Cuerpo("CLUB");

        var primera = await Actividades.LeerAsync(await e.P.PostAsync(e.Ruta(), cuerpo), HttpStatusCode.Created);
        var segunda = await Actividades.LeerAsync(await e.P.PostAsync(e.Ruta(), cuerpo), HttpStatusCode.OK);

        Assert.Equal(primera.GetProperty("id").GetGuid(), segunda.GetProperty("id").GetGuid());
        Assert.Equal(1, await ContarAsync(e.Club.Club.Id));
        Assert.Single(await e.RecibidosAsync(e.Ana));
    }

    [Fact]
    public async Task Dos_peticiones_simultaneas_con_la_misma_clave_crean_uno_solo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var cuerpo = Cuerpo("CLUB");

        var respuestas = await Task.WhenAll(
            e.P.PostAsync(e.Ruta(), cuerpo), e.P.PostAsync(e.Ruta(), cuerpo), e.P.PostAsync(e.Ruta(), cuerpo));

        Assert.All(respuestas, respuesta => Assert.True(
            respuesta.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, respuesta.StatusCode.ToString()));
        var ids = new List<Guid>();
        foreach (var respuesta in respuestas)
        {
            ids.Add((await Actividades.LeerAsync(respuesta, respuesta.StatusCode)).GetProperty("id").GetGuid());
        }

        Assert.Single(ids.Distinct());
        Assert.Equal(1, await ContarAsync(e.Club.Club.Id));
        Assert.Single(await e.RecibidosAsync(e.Ana));
    }

    [Fact]
    public async Task La_clave_de_otra_persona_responde_409()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var clave = Guid.NewGuid();
        await e.PublicarAsync(e.P, Cuerpo("CLUB", clave: clave));

        await Actividades.ErrorAsync(
            await e.D.PostAsync(e.Ruta(), Cuerpo("CLUB", clave: clave)), HttpStatusCode.Conflict, "clave_en_uso");
        Assert.Equal(1, await ContarAsync(e.Club.Club.Id));
    }

    [Fact]
    public async Task Dos_comunicados_con_el_mismo_texto_y_otra_clave_son_dos_comunicados()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        await e.PublicarAsync(e.P, Cuerpo("CLUB"));
        await e.PublicarAsync(e.P, Cuerpo("CLUB"));

        Assert.Equal(2, await ContarAsync(e.Club.Club.Id));
    }
}
