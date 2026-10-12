using System.Net;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LFClub.Pruebas.Integracion.Comunicados;

/// <summary>
/// Publicar un comunicado: quién puede, qué guarda, qué valida y quién lo recibe (spec 011, bloque 1
/// de la guía; RF-001 a RF-011).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PublicarComunicadoPruebas
{
    private readonly FabricaApi _fabrica;

    public PublicarComunicadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_publica_a_todo_el_club_y_llega_a_las_cuentas_con_un_jugador_activo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var respuesta = await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", titulo: "  Reunión  ", texto: "  Es el viernes.  "));
        var publicado = await Actividades.LeerAsync(respuesta, HttpStatusCode.Created);

        Assert.Equal("Reunión", publicado.GetProperty("titulo").GetString());
        Assert.Equal("Es el viernes.", publicado.GetProperty("texto").GetString());
        Assert.Equal("CLUB", publicado.GetProperty("alcance").GetString());
        Assert.Empty(publicado.GetProperty("destinos").EnumerateArray());
        Assert.Equal("PRESIDENTE", publicado.GetProperty("publicadoPor").GetProperty("rol").GetString());
        Assert.Equal(
            e.Club.Presidente.Integrante.Id, publicado.GetProperty("publicadoPor").GetProperty("usuarioRolId").GetGuid());
        Assert.True(publicado.GetProperty("puedeRetirar").GetBoolean());
        Assert.False(publicado.TryGetProperty("retirado", out _));
        // Ana (con su hermano en espera), Beto, Caro y Dani; no Eva, que está retirada.
        Assert.Equal(4, publicado.GetProperty("familiasAlcanzadas").GetInt32());

        var id = publicado.GetProperty("id").GetGuid();
        foreach (var familia in new[] { e.Ana, e.Beto, e.Caro, e.Dani })
        {
            Assert.Contains(id, await e.RecibidosAsync(familia));
        }

        // Eva está retirada: ni siquiera abre la aplicación, y no tiene fila de destinatario.
        await Actividades.ErrorAsync(
            await e.Eva.GetAsync(e.RutaCampana()), HttpStatusCode.Forbidden, "integrante_retirado");
        Assert.Equal(0, await e.FilasDeAsync(id, e.Club.Eva.Integrante));
    }

    [Fact]
    public async Task El_directivo_y_el_entrenador_tambien_publican()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var deD = await e.PublicarAsync(e.D, Cuerpo("CLUB"));
        var deE1 = await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]));

        Assert.Equal("DIRECTIVO", deD.GetProperty("publicadoPor").GetProperty("rol").GetString());
        Assert.Equal("ENTRENADOR", deE1.GetProperty("publicadoPor").GetProperty("rol").GetString());
        Assert.Equal("CATEGORIAS", deE1.GetProperty("alcance").GetString());
        Assert.Equal("2014", deE1.GetProperty("destinos")[0].GetProperty("categoriaNombre").GetString());
        // La 2014 tiene a Ana y a Beto.
        Assert.Equal(2, deE1.GetProperty("familiasAlcanzadas").GetInt32());
    }

    [Fact]
    public async Task Un_comunicado_a_equipos_guarda_cada_equipo_con_su_categoria()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var publicado = await e.PublicarAsync(e.P, Cuerpo("EQUIPOS", equipos: [e.Club.EquipoA, e.Club.EquipoB]));

        var destinos = publicado.GetProperty("destinos").EnumerateArray().ToList();
        Assert.Equal(2, destinos.Count);
        Assert.All(destinos, destino => Assert.Equal("2014", destino.GetProperty("categoriaNombre").GetString()));
        Assert.Equal(
            new[] { "A", "B" },
            destinos.Select(destino => destino.GetProperty("equipoNombre").GetString()!).Order().ToArray());
        // Solo Ana juega en A y B.
        Assert.Equal(1, publicado.GetProperty("familiasAlcanzadas").GetInt32());
    }

    [Fact]
    public async Task Jugador_y_entrenador_sin_ficha_de_equipo_reciben_403_o_404()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        await Actividades.ErrorAsync(
            await e.Ana.PostAsync(e.Ruta(), Cuerpo("CLUB")), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(
            await desarrollador.PostAsync(e.Ruta(), Cuerpo("CLUB")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Titulo_y_texto_vacios_o_demasiado_largos_responden_400_por_campo()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", titulo: "   ")), HttpStatusCode.BadRequest, "datos_invalidos");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", texto: "")), HttpStatusCode.BadRequest, "datos_invalidos");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", titulo: new string('a', 101))),
            HttpStatusCode.BadRequest,
            "datos_invalidos");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", texto: new string('a', 1001))),
            HttpStatusCode.BadRequest,
            "datos_invalidos");

        var limite = await e.P.PostAsync(
            e.Ruta(), Cuerpo("CLUB", titulo: new string('a', 100), texto: new string('b', 1000)));
        Assert.Equal(HttpStatusCode.Created, limite.StatusCode);
    }

    [Fact]
    public async Task El_texto_se_guarda_tal_cual_sin_interpretar()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        const string texto = "<script>alert(1)</script> **negrita** [enlace](http://x)\nsegunda línea";

        var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB", texto: texto));

        Assert.Equal(texto, publicado.GetProperty("texto").GetString());
    }

    [Fact]
    public async Task Un_alcance_inventado_responde_400()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var respuesta = await e.P.PostAsync(e.Ruta(), Cuerpo("TODOS"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
