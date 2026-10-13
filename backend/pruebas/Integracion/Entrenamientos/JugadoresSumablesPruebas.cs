using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// La búsqueda de jugadores que se pueden sumar: acotada a 20, con lo mínimo para elegirlos, solo
/// para quien dirige, sin retirados y sin revelar nada de otro club (spec 013, historia 3; RF-013,
/// RF-016; research §9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class JugadoresSumablesPruebas
{
    private readonly FabricaApi _fabrica;

    public JugadoresSumablesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Ruta(EscenarioActividades e, string busqueda) =>
        e.Ruta($"/actividades/jugadores-sumables?busqueda={Uri.EscapeDataString(busqueda)}");

    [Fact]
    public async Task Devuelve_solo_nombres_apellidos_y_categoria_de_quien_coincide()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var encontrados = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(Ruta(e, "Kapellido")));

        var k1 = Assert.Single(encontrados.EnumerateArray());
        Assert.Equal(e.K1.Id, k1.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal("K1", k1.GetProperty("nombres").GetString());
        Assert.Equal(2015, k1.GetProperty("categoria").GetProperty("anio").GetInt32());
        var propiedades = k1.EnumerateObject().Select(propiedad => propiedad.Name).Order().ToList();
        Assert.Equal(["apellidos", "categoria", "nombres", "usuarioRolId"], propiedades);
    }

    [Fact]
    public async Task Devuelve_veinte_como_maximo_ordenados_por_apellidos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        for (var numero = 0; numero < 25; numero++)
        {
            await _fabrica.Categorias.CrearJugadorAsync(e.Club, 2014, e.C2014, "Lote", $"Zeta{numero:00}");
        }

        var encontrados = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(Ruta(e, "Zeta")));

        Assert.Equal(20, encontrados.GetArrayLength());
        var apellidos = encontrados.EnumerateArray().Select(fila => fila.GetProperty("apellidos").GetString()!).ToList();
        Assert.Equal(apellidos.Order(StringComparer.Ordinal).ToList(), apellidos);
    }

    [Fact]
    public async Task Con_una_sola_letra_responde_400_y_un_retirado_no_aparece()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Categorias.RetirarAsync(e.K1.Integrante, e.P.Integrante);

        await Actividades.ErrorAsync(
            await e.E1.Cliente.GetAsync(Ruta(e, "K")), HttpStatusCode.BadRequest, "datos_invalidos");
        var encontrados = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(Ruta(e, "Kapellido")));

        Assert.Equal(JsonValueKind.Array, encontrados.ValueKind);
        Assert.Equal(0, encontrados.GetArrayLength());
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_403_y_el_directivo_sin_asignar_una_lista_vacia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.GetAsync(Ruta(e, "Apellido")), HttpStatusCode.Forbidden, "rol_no_autorizado");
        var deD2 = await Actividades.LeerAsync(await e.D2.Cliente.GetAsync(Ruta(e, "Apellido")));
        var deD1 = await Actividades.LeerAsync(await e.D1.Cliente.GetAsync(Ruta(e, "Apellido")));

        Assert.Equal(0, deD2.GetArrayLength());
        Assert.True(deD1.GetArrayLength() > 0);
    }

    [Fact]
    public async Task No_devuelve_jugadores_de_otro_club_ni_responde_al_desarrollador()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var encontrados = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(Ruta(e, "Bapellido")));
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delOtroClub = await Actividades.LeerAsync(await e.PB.Cliente.GetAsync(e.RutaB("/actividades/jugadores-sumables?busqueda=Apellido")));

        Assert.Equal(0, encontrados.GetArrayLength());
        Assert.DoesNotContain(
            delOtroClub.EnumerateArray(), fila => fila.GetProperty("usuarioRolId").GetGuid() == e.Jn(3).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await desarrollador.GetAsync(Ruta(e, "Apellido"))).StatusCode);
        Assert.Equal(Rol.PRESIDENTE, e.PB.Integrante.Rol);
    }
}
