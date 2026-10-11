using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LaPecosa.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LaPecosa.Pruebas.Integracion.Personas;

/// <summary>
/// Todos los roles del club ven la misma ficha básica, también la familia de un jugador de otra
/// categoría; la familia no abre fichas básicas de jugadores, y quien no puede entrar en el club
/// recibe los 403 de siempre (spec 008, RF-003 y RF-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaBasicaPorRolPruebas
{
    private readonly FabricaApi _fabrica;

    public FichaBasicaPorRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Todos_los_roles_del_club_ven_la_misma_ficha_de_E1()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var quienes = new[]
        {
            await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante),
            await ClienteAsync(c.Caro.Usuario),
            await ClienteAsync(c.E2.Usuario),
            await ClienteAsync(c.D.Usuario),
            await ClienteAsync(c.P.Usuario),
        };

        var fichas = new List<string>();
        foreach (var cliente in quienes)
        {
            fichas.Add((await LeerAsync(cliente, c.Ficha(c.E1.Integrante))).GetRawText());
        }

        Assert.Single(fichas.Distinct());
        Assert.Contains("\"ENTRENADOR\"", fichas[0]);
    }

    [Fact]
    public async Task La_familia_no_abre_la_ficha_basica_de_un_jugador_ni_la_suya()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var familia = await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);

        await NoEncontradoAsync(await familia.GetAsync(c.Ficha(c.Caro.Integrante)));
        await NoEncontradoAsync(await familia.GetAsync(c.Ficha(c.Ana.Integrante)));
    }

    [Fact]
    public async Task En_espera_y_retirado_reciben_los_403_de_siempre()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var conLuis = await ClienteAsync(c.Ana.Usuario, c.Luis);
        var eva = await ClienteAsync(c.Eva.Usuario);

        var enEspera = await conLuis.GetAsync(c.Ficha(c.E1.Integrante));
        var retirada = await eva.GetAsync(c.Ficha(c.E1.Integrante));

        Assert.Equal(HttpStatusCode.Forbidden, enEspera.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(enEspera));
        Assert.Equal(HttpStatusCode.Forbidden, retirada.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(retirada));
    }

    [Fact]
    public async Task Con_el_club_suspendido_E1_no_entra_y_el_presidente_la_abre()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.ConContextoAsync(contexto => contexto.Clubes.Where(club => club.Id == c.Club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(club => club.Estado, EstadoClub.SUSPENDIDO)));

        var e1 = await (await ClienteAsync(c.E1.Usuario)).GetAsync(c.Ficha(c.E2.Integrante));
        var presidente = await LeerAsync(await ClienteAsync(c.P.Usuario), c.Ficha(c.E1.Integrante));

        Assert.Equal(HttpStatusCode.Forbidden, e1.StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(e1));
        Assert.Equal("ENTRENADOR", presidente.GetProperty("rol").GetString());
    }

    private Task<ClienteDePrueba> ClienteAsync(Usuario cuenta, UsuarioRol? elegido = null) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(elegido?.Id).ConSesionDeAsync(cuenta);
}
