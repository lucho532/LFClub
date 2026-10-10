using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Inicio;

/// <summary>
/// El resumen del inicio del PRESIDENTE y de los DIRECTIVOS cuenta exactamente lo que el club tiene
/// hoy, solo en su club, y nadie más lo recibe (spec 007, historia 3; RF-011 y RF-015; CE-003).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ResumenClubPruebas
{
    private readonly FabricaApi _fabrica;

    public ResumenClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_club_nuevo_tiene_las_cuatro_cifras_en_cero()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);

        var resumen = await ResumenAsync(await _fabrica.Jugadores.ClienteAsync(presidente), $"/api/clubes/{club.Id}/resumen");

        Assert.Equal((0, 0, 0, 0), resumen);
    }

    [Fact]
    public async Task Cuenta_lo_del_club_sin_repetir_entrenadores_y_sin_nada_desactivado_ni_de_otro_club()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Jugadores.CrearClubAsync();
        // E1 con dos asignaciones sigue siendo una persona.
        await _fabrica.Categorias.AsignarEntrenadorAsync(c.C2015, c.E1.Integrante);

        foreach (var cuenta in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var resumen = await ResumenAsync(await _fabrica.Jugadores.ClienteAsync(cuenta), c.Ruta("/resumen"));

            // Ana, Beto, Caro y Dani; A, B y C (no D, de la 2016 desactivada); E1, E2 y el directivo
            // que entrena la 2015; 2014 y 2015.
            Assert.Equal((4, 3, 3, 2), resumen);
        }
    }

    [Fact]
    public async Task Al_desactivar_un_equipo_o_retirar_a_un_jugador_la_cifra_baja()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var presidente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);
        await _fabrica.ConContextoAsync(contexto => contexto.Equipos.IgnoreQueryFilters()
            .Where(equipo => equipo.Id == c.EquipoB.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Activo, false)));
        Assert.Equal(HttpStatusCode.NoContent, (await presidente.PostAsync(c.Ruta($"/jugadores/{c.Beto.Integrante.Id}/retiro"))).StatusCode);

        var resumen = await ResumenAsync(presidente, c.Ruta("/resumen"));

        Assert.Equal((3, 2, 3, 2), resumen);
    }

    [Fact]
    public async Task El_entrenador_y_la_cuenta_de_un_jugador_reciben_403()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var quienes = new[]
        {
            await _fabrica.Jugadores.ClienteAsync(c.E1.Usuario),
            await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante),
        };

        foreach (var cliente in quienes)
        {
            var respuesta = await cliente.GetAsync(c.Ruta("/resumen"));

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    private static async Task<(int, int, int, int)> ResumenAsync(ClienteDePrueba cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resumen = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(["jugadores", "equipos", "entrenadores", "categorias"], resumen.EnumerateObject().Select(campo => campo.Name));
        return (
            resumen.GetProperty("jugadores").GetInt32(),
            resumen.GetProperty("equipos").GetInt32(),
            resumen.GetProperty("entrenadores").GetInt32(),
            resumen.GetProperty("categorias").GetInt32());
    }
}
