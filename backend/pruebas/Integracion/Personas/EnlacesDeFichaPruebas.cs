using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Personas;

/// <summary>
/// Los nombres copiados en el último cambio, los retiros y los ingresos llevan a la ficha de la
/// persona de hoy mientras siga en el club, y son texto cuando no; el nombre copiado no cambia
/// (spec 008, historia 2; RF-010 a RF-012; CE-005; constitución §13).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EnlacesDeFichaPruebas
{
    private readonly FabricaApi _fabrica;

    public EnlacesDeFichaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_ultimo_cambio_por_P_lleva_a_su_ficha_basica_para_todos_los_que_ven_la_ficha()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var quienes = new[]
        {
            await ClienteAsync(c.P.Usuario),
            await ClienteAsync(c.D.Usuario),
            await ClienteAsync(c.E1.Usuario),
            await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante),
        };

        foreach (var cliente in quienes)
        {
            var cambio = (await LeerAsync(cliente, FichaDe(c, c.Ana.Integrante))).GetProperty("ultimoCambio");

            Assert.Equal($"{c.P.Integrante.Nombres} {c.P.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
            ComprobarEnlace(cambio.GetProperty("autorFicha"), c.P.Integrante.Id, "BASICA");
        }
    }

    [Fact]
    public async Task Tras_quitar_a_P_del_club_el_autor_sigue_con_su_nombre_y_sin_enlace()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.QuitarDelClubAsync(c.P.Integrante);

        var cambio = (await LeerAsync(await ClienteAsync(c.D.Usuario), FichaDe(c, c.Ana.Integrante))).GetProperty("ultimoCambio");

        Assert.Equal($"{c.P.Integrante.Nombres} {c.P.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
        Assert.Equal(JsonValueKind.Null, cambio.GetProperty("autorFicha").ValueKind);
    }

    [Fact]
    public async Task Un_cambio_de_la_familia_no_lleva_enlace()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Fichas.CrearFichaAsync(c.Caro.Integrante);

        var cambio = (await LeerAsync(await ClienteAsync(c.P.Usuario), FichaDe(c, c.Caro.Integrante))).GetProperty("ultimoCambio");

        Assert.True(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
        Assert.Equal(JsonValueKind.Null, cambio.GetProperty("autorFicha").ValueKind);
    }

    [Fact]
    public async Task Si_P_cambia_sus_apellidos_el_autor_sigue_con_el_nombre_de_entonces()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(fila => fila.Id == c.P.Integrante.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Apellidos, "Apellido Nuevo")));

        var cambio = (await LeerAsync(await ClienteAsync(c.D.Usuario), FichaDe(c, c.Ana.Integrante))).GetProperty("ultimoCambio");

        Assert.Equal($"{c.P.Integrante.Nombres} {c.P.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
        ComprobarEnlace(cambio.GetProperty("autorFicha"), c.P.Integrante.Id, "BASICA");
    }

    [Fact]
    public async Task La_familia_no_obtiene_la_ficha_de_otro_jugador_aunque_tenga_su_identificador()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var familia = await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);

        await NoEncontradoAsync(await familia.GetAsync(FichaDe(c, c.Caro.Integrante)));
    }

    [Fact]
    public async Task Eva_retirada_por_P_lleva_a_su_ficha_basica()
    {
        var c = await _fabrica.Personas.CrearAsync();

        var eva = await RetiradaAsync(c, c.Eva.Integrante);

        Assert.Equal($"{c.P.Integrante.Nombres} {c.P.Integrante.Apellidos}", eva.GetProperty("retiradoPor").GetString());
        ComprobarEnlace(eva.GetProperty("retiradoPorFicha"), c.P.Integrante.Id, "BASICA");
    }

    [Fact]
    public async Task Un_retiro_hecho_por_la_familia_no_lleva_enlace()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Categorias.RetirarAsync(c.Caro.Integrante, c.Caro.Integrante);

        var caro = await RetiradaAsync(c, c.Caro.Integrante);

        Assert.Equal(JsonValueKind.Null, caro.GetProperty("retiradoPorFicha").ValueKind);
    }

    [Fact]
    public async Task Tras_quitar_a_P_del_club_el_retiro_queda_sin_enlace_y_con_el_nombre_copiado()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.QuitarDelClubAsync(c.P.Integrante);

        var eva = await RetiradaAsync(c, c.Eva.Integrante);

        Assert.Equal($"{c.P.Integrante.Nombres} {c.P.Integrante.Apellidos}", eva.GetProperty("retiradoPor").GetString());
        Assert.Equal(JsonValueKind.Null, eva.GetProperty("retiradoPorFicha").ValueKind);
    }

    private async Task<JsonElement> RetiradaAsync(ClubDePersonas c, UsuarioRol jugador) =>
        (await LeerAsync(await ClienteAsync(c.D.Usuario), c.Ruta("/jugadores/retirados"))).EnumerateArray()
            .Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == jugador.Id);

    private static string FichaDe(ClubDePersonas c, UsuarioRol jugador) => c.Ruta($"/jugadores/{jugador.Id}/ficha");

    private Task<ClienteDePrueba> ClienteAsync(Usuario cuenta, UsuarioRol? elegido = null) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(elegido?.Id).ConSesionDeAsync(cuenta);
}
