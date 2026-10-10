using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Una cuenta con varios jugadores en el club recibe una sola entrada del club con sus jugadores y
/// dice con cuál continúa en cada petición; una cuenta con uno solo no envía nada (historia 2,
/// escenarios 1 a 3 y 8 a 10; RF-013, RF-021 a RF-024 y RF-029; CE-006 y CE-009).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ElegirJugadorPruebas
{
    private readonly FabricaApi _fabrica;

    public ElegirJugadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static async Task<JsonElement> ClubDeLaSesionAsync(ClienteDePrueba cliente, Guid clubId)
    {
        var sesion = await (await cliente.GetAsync("/api/sesion")).JsonAsync();
        return sesion.Lista("clubes").Single(club => club.GetProperty("clubId").GetGuid() == clubId);
    }

    // Escenario 2.1 y RF-022.
    [Fact]
    public async Task La_sesion_trae_una_sola_entrada_del_club_con_sus_jugadores_por_antiguedad_y_su_estado()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Luis");
        var retirado = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, activo: false, nombres: "Mara");
        var familia = await e.ClienteDeLaFamiliaAsync();

        var club = await ClubDeLaSesionAsync(familia, e.Club.Id);

        var jugadores = club.Lista("jugadores");
        Assert.Equal([e.Ana.Id, enEspera.Id, retirado.Id], jugadores.Ids("usuarioRolId"));
        Assert.Equal(["Ana", "Luis", "Mara"], jugadores.Select(jugador => jugador.GetProperty("nombres").GetString()));
        Assert.All(jugadores, jugador => Assert.Equal(e.Ana.Apellidos, jugador.GetProperty("apellidos").GetString()));
        Assert.Equal(
            ["APROBADO", "EN_ESPERA", "APROBADO"],
            jugadores.Select(jugador => jugador.GetProperty("estadoIngreso").GetString()));
        Assert.Equal([false, false, true], jugadores.Select(jugador => jugador.GetProperty("retirado").GetBoolean()));

        // Los campos de la 001 describen al más antiguo hasta que la familia elija.
        Assert.Equal(e.Ana.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal("Ana", club.GetProperty("nombres").GetString());
        Assert.Equal("APROBADO", club.GetProperty("estadoIngreso").GetString());
        Assert.False(club.GetProperty("retirado").GetBoolean());
    }

    // Escenario 2.3 y RF-023.
    [Fact]
    public async Task Una_cuenta_con_un_solo_jugador_recibe_la_lista_vacia_y_su_identificador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var deBeto = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(e.CuentaDeBeto);

        var club = await ClubDeLaSesionAsync(deBeto, e.Club.Id);

        Assert.Empty(club.Lista("jugadores"));
        Assert.Equal(e.Beto.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await deBeto.GetAsync(e.InicioDelClub)).StatusCode);
    }

    // Escenario 2.2 y RF-021.
    [Fact]
    public async Task Sin_cabecera_hay_que_elegir_y_con_la_de_cada_jugador_la_peticion_es_suya()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var familia = await e.ClienteDeLaFamiliaAsync();

        var sinElegir = await familia.GetAsync(e.InicioDelClub);

        Assert.Equal(HttpStatusCode.Conflict, sinElegir.StatusCode);
        Assert.Equal("jugador_sin_elegir", await ClienteDePrueba.CodigoAsync(sinElegir));
        Assert.DoesNotContain(e.Club.Nombre, await sinElegir.Content.ReadAsStringAsync());

        foreach (var jugador in new[] { e.Ana, luis })
        {
            var respuesta = await familia.ElegirJugador(jugador.Id).GetAsync(e.InicioDelClub);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(jugador.Id, (await respuesta.JsonAsync()).GetProperty("miUsuarioRolId").GetGuid());
        }
    }

    // Escenario 2.8 y RF-029: el mismo 404 para un jugador ajeno, uno de otro club y un texto cualquiera.
    [Fact]
    public async Task Elegir_un_jugador_que_no_es_de_la_cuenta_en_el_club_responde_como_si_no_existiera()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var enOtroClub = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Familia, Rol.JUGADOR);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var inexistente = await familia.ElegirJugador(Guid.NewGuid()).GetAsync(e.InicioDelClub);
        string[] ajenos = [e.Beto.Id.ToString(), enOtroClub.Id.ToString(), "no-es-un-identificador", "123"];

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        foreach (var ajeno in ajenos)
        {
            var respuesta = await familia.ElegirJugador(ajeno).GetAsync(e.InicioDelClub);

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{ajeno}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await respuesta.Content.ReadAsStringAsync());
        }

        // La cabecera vacía es no haber elegido.
        Assert.Equal(HttpStatusCode.Conflict, (await familia.ElegirJugador(" ").GetAsync(e.InicioDelClub)).StatusCode);
    }

    [Fact]
    public async Task Una_cuenta_de_un_solo_jugador_puede_enviar_su_identificador_y_ningun_otro()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var deBeto = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(e.CuentaDeBeto);

        var conElSuyo = await deBeto.ElegirJugador(e.Beto.Id).GetAsync(e.InicioDelClub);
        var conOtro = await deBeto.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub);
        var malFormado = await deBeto.ElegirJugador("beto").GetAsync(e.InicioDelClub);

        Assert.Equal(HttpStatusCode.OK, conElSuyo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, conOtro.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(conOtro));
        Assert.Equal(HttpStatusCode.NotFound, malFormado.StatusCode);
    }

    // Escenarios 2.9 y 2.10 y RF-013.
    [Fact]
    public async Task Elegir_al_hermano_en_espera_o_al_retirado_no_entrega_ningun_dato_del_club()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var retirado = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, activo: false);
        var familia = await e.ClienteDeLaFamiliaAsync();

        var delEnEspera = await familia.ElegirJugador(enEspera.Id).GetAsync(e.InicioDelClub);
        var delRetirado = await familia.ElegirJugador(retirado.Id).GetAsync(e.InicioDelClub);
        var deAna = await familia.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub);

        Assert.Equal(HttpStatusCode.Forbidden, delEnEspera.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(delEnEspera));
        Assert.Equal(HttpStatusCode.Forbidden, delRetirado.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(delRetirado));
        foreach (var respuesta in new[] { delEnEspera, delRetirado })
        {
            Assert.DoesNotContain(e.Club.Nombre, await respuesta.Content.ReadAsStringAsync());
        }

        // RF-014: los demás jugadores de la cuenta siguen con normalidad.
        Assert.Equal(HttpStatusCode.OK, deAna.StatusCode);
    }

    // RF-024 y CE-009.
    [Fact]
    public async Task Mi_categoria_es_la_del_jugador_elegido()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var categoria2015 = await _fabrica.Categorias.CrearCategoriaAsync(e.Club, 2015);
        var luis = await e.Sembrar.CrearHermanoAsync(
            e.Ana, EstadoIngreso.APROBADO, new DateOnly(2015, 4, 2), categoria: categoria2015);
        var sinCategoria = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, new DateOnly(2016, 4, 2));
        var familia = await e.ClienteDeLaFamiliaAsync();

        var deAna = await (await familia.ElegirJugador(e.Ana.Id).GetAsync(e.Base.MiCategoria)).JsonAsync();
        var deLuis = await (await familia.ElegirJugador(luis.Id).GetAsync(e.Base.MiCategoria)).JsonAsync();
        var delTercero = await (await familia.ElegirJugador(sinCategoria.Id).GetAsync(e.Base.MiCategoria)).JsonAsync();

        Assert.Equal(EscenarioHermanos.AnioDeAna, deAna.GetProperty("categoria").GetProperty("anio").GetInt32());
        Assert.Equal(2015, deLuis.GetProperty("categoria").GetProperty("anio").GetInt32());
        Assert.Equal(JsonValueKind.Null, delTercero.GetProperty("categoria").ValueKind);
    }
}
