using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using static LFClub.Pruebas.Integracion.Comunicados.EscenarioComunicados;

namespace LFClub.Pruebas.Integracion.Comunicados;

/// <summary>
/// Quién recibe cada audiencia y a cuáles puede dirigirse cada rol, con cuentas de varias familias
/// (spec 011, CE-001 y CE-002; RF-004 a RF-009).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AudienciaPruebas
{
    private readonly FabricaApi _fabrica;

    public AudienciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Cada_audiencia_llega_solo_a_quien_corresponde()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        var aLa2014 = (await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2014]))).GetProperty("id").GetGuid();
        var aLa2015 = (await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2015]))).GetProperty("id").GetGuid();
        var alEquipoA = (await e.PublicarAsync(e.P, Cuerpo("EQUIPOS", equipos: [e.Club.EquipoA]))).GetProperty("id").GetGuid();
        var aTodos = (await e.PublicarAsync(e.P, Cuerpo("CLUB"))).GetProperty("id").GetGuid();

        var deAna = await e.RecibidosAsync(e.Ana);
        var deBeto = await e.RecibidosAsync(e.Beto);
        var deCaro = await e.RecibidosAsync(e.Caro);
        var deDani = await e.RecibidosAsync(e.Dani);

        Assert.Equal(new[] { aLa2014, alEquipoA, aTodos }.Order(), deAna.Order());
        Assert.DoesNotContain(aLa2015, deAna);
        Assert.Contains(aLa2014, deBeto);
        Assert.DoesNotContain(alEquipoA, deBeto);
        Assert.Contains(aLa2015, deCaro);
        Assert.DoesNotContain(aLa2014, deCaro);
        // Dani no tiene categoría: solo recibe lo de todo el club.
        Assert.Equal([aTodos], deDani);
        // Eva está retirada: no tiene fila de destinatario en ninguno.
        foreach (var id in new[] { aLa2014, aLa2015, alEquipoA, aTodos })
        {
            Assert.Equal(0, await e.FilasDeAsync(id, e.Club.Eva.Integrante));
        }
    }

    [Fact]
    public async Task Un_hermano_en_espera_no_cuenta_como_destinatario_y_el_activo_recibe_una_vez()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var luis = e.Club.Luis;

        var publicado = await e.PublicarAsync(e.P, Cuerpo("CLUB"));

        // Luis (en espera) no cuenta como destinatario; la cuenta de Ana lo recibe por Ana, una vez.
        Assert.Equal(1, (await e.RecibidosAsync(e.Ana)).Count(id => id == publicado.GetProperty("id").GetGuid()));
        Assert.Equal(4, publicado.GetProperty("familiasAlcanzadas").GetInt32());
        Assert.Equal(EstadoIngreso.EN_ESPERA, luis.EstadoIngreso);
    }

    [Fact]
    public async Task Los_hermanos_en_dos_categorias_reciben_una_sola_vez()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");

        var publicado = await e.PublicarAsync(e.P, Cuerpo("CATEGORIAS", [e.Club.C2014, e.Club.C2015]));

        var lista = await e.RecibidosAsync(e.Ana);
        Assert.Equal([publicado.GetProperty("id").GetGuid()], lista);
        Assert.Equal(1, await e.SinLeerAsync(e.Ana));
        // Ana, Hugo (misma cuenta), Beto y Caro: tres cuentas.
        Assert.Equal(3, publicado.GetProperty("familiasAlcanzadas").GetInt32());
    }

    [Fact]
    public async Task El_entrenador_solo_publica_a_sus_categorias_y_a_sus_equipos_y_nunca_a_todo_el_club()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        await e.PublicarAsync(e.E1, Cuerpo("CATEGORIAS", [e.Club.C2014]));
        // Cualquier equipo activo de su categoría, no solo el que dirige.
        await e.PublicarAsync(e.E1, Cuerpo("EQUIPOS", equipos: [e.Club.EquipoA, e.Club.EquipoB]));

        await Actividades.ErrorAsync(
            await e.E1.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [e.Club.C2015])),
            HttpStatusCode.Forbidden,
            "no_entrena_categoria");
        await Actividades.ErrorAsync(
            await e.E1.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [e.Club.C2014, e.Club.C2015])),
            HttpStatusCode.Forbidden,
            "no_entrena_categoria");
        await Actividades.ErrorAsync(
            await e.E1.PostAsync(e.Ruta(), Cuerpo("EQUIPOS", equipos: [e.Club.EquipoC])),
            HttpStatusCode.Forbidden,
            "no_entrena_categoria");
        await Actividades.ErrorAsync(
            await e.E1.PostAsync(e.Ruta(), Cuerpo("CLUB")), HttpStatusCode.Forbidden, "no_entrena_categoria");
        await Actividades.ErrorAsync(
            await e.E2.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [e.Club.C2014])),
            HttpStatusCode.Forbidden,
            "no_entrena_categoria");
    }

    [Fact]
    public async Task El_directivo_asignado_como_entrenador_publica_con_el_alcance_de_su_rol()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        // El directivo entrena la 2015 y aun así puede dirigirse a la 2014 y a todo el club.
        await e.PublicarAsync(e.D, Cuerpo("CATEGORIAS", [e.Club.C2014]));
        await e.PublicarAsync(e.D, Cuerpo("CLUB"));
    }

    [Fact]
    public async Task Las_audiencias_incoherentes_o_inexistentes_responden_400()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otroClub = await _fabrica.Jugadores.CrearClubAsync();

        // Equipos de dos categorías.
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("EQUIPOS", equipos: [e.Club.EquipoA, e.Club.EquipoC])),
            HttpStatusCode.BadRequest,
            "audiencia_invalida");
        // Categorías sin elegir y todo el club con categorías.
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS")), HttpStatusCode.BadRequest, "audiencia_invalida");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CLUB", [e.Club.C2014])), HttpStatusCode.BadRequest, "audiencia_invalida");
        // Categoría inactiva y categoría de otro club.
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [e.Club.C2016])),
            HttpStatusCode.BadRequest,
            "audiencia_invalida");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [otroClub.C2014])),
            HttpStatusCode.BadRequest,
            "audiencia_invalida");
        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("EQUIPOS", equipos: [otroClub.EquipoA])),
            HttpStatusCode.BadRequest,
            "audiencia_invalida");
    }

    [Fact]
    public async Task Una_audiencia_sin_jugadores_activos_responde_400_y_no_guarda_nada()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var vacia = await _fabrica.Categorias.CrearCategoriaAsync(e.Club.Club, 2020);

        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.Ruta(), Cuerpo("CATEGORIAS", [vacia])), HttpStatusCode.BadRequest, "sin_destinatarios");

        var lista = await Actividades.LeerAsync(await e.P.GetAsync(e.Ruta()));
        Assert.Empty(lista.GetProperty("items").EnumerateArray());
    }
}
