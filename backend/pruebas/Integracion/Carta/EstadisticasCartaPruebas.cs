using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LaPecosa.Pruebas.Integracion.Carta.ConsultaCartaPruebas;

namespace LaPecosa.Pruebas.Integracion.Carta;

/// <summary>
/// Las estadísticas de la carta las ponen quien entrena la categoría activa del jugador y el
/// PRESIDENTE; la valoración se calcula (spec 010, escenarios 3.2, 3.3 y 3.5, casos límite 7 y 8;
/// RF-021 a RF-024, RF-028; CE-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EstadisticasCartaPruebas
{
    private readonly FabricaApi _fabrica;

    public EstadisticasCartaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Ruta(EscenarioActividades e, Persona jugador) => $"{RutaCarta(e, jugador)}/estadisticas";

    private static object Seis(object? ritmo, object? tiro, object? pase, object? regate, object? defensa, object? fisico) =>
        new { ritmo, tiro, pase, regate, defensa, fisico };

    private static readonly object Buenas = Seis(70, 65, 80, 72, 60, 88);

    [Fact]
    public async Task Quien_entrena_las_pone_y_la_valoracion_redondea_hacia_arriba()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var cifrasAntes = await (await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{j3.Id}/estadisticas"))).Content.ReadAsStringAsync();

        var carta = await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(Ruta(e, j3), Buenas));

        Assert.Equal(73, carta.GetProperty("valoracion").GetInt32());
        Assert.Equal($"{e.E1.Integrante.Nombres} {e.E1.Integrante.Apellidos}", carta.GetProperty("ultimoCambioEstadisticas").GetProperty("autor").GetString());
        Assert.Equal(e.E1.Id, carta.GetProperty("ultimoCambioEstadisticas").GetProperty("autorFicha").GetProperty("usuarioRolId").GetGuid());
        var cifrasDespues = await (await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{j3.Id}/estadisticas"))).Content.ReadAsStringAsync();
        Assert.Equal(cifrasAntes, cifrasDespues);

        await Actividades.LeerAsync(await e.D1.Cliente.PutAsync(Ruta(e, j3), Seis(99, 99, 99, 99, 99, 99)));
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(Ruta(e, j3), Buenas));

        // Las seis nulas la dejan sin números.
        var sinNumeros = await e.E1.Cliente.PutAsync(Ruta(e, j3), Seis(null, null, null, null, null, null));
        var texto = await sinNumeros.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, sinNumeros.StatusCode);
        Assert.DoesNotContain("\"valoracion\"", texto);
        Assert.DoesNotContain("\"estadisticas\"", texto);
    }

    public static TheoryData<object> Invalidas => new()
    {
        Seis(0, 65, 80, 72, 60, 88),
        Seis(70, 65, 80, 72, 60, 100),
        Seis(70, 65, 50.5, 72, 60, 88),
        Seis(70, 65, 80, 72, 60, null),
    };

    [Theory]
    [MemberData(nameof(Invalidas))]
    public async Task Fuera_de_rango_decimales_o_incompletas_responden_400(object datos)
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        await Actividades.ErrorAsync(await e.E1.Cliente.PutAsync(Ruta(e, e.Jn(3)), datos), HttpStatusCode.BadRequest, "datos_invalidos");
    }

    [Fact]
    public async Task Quien_no_entrena_su_categoria_o_la_familia_no_las_cambian()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        await Actividades.ErrorAsync(await e.D2.Cliente.PutAsync(Ruta(e, e.Jn(3)), Buenas), HttpStatusCode.Forbidden, "no_entrena_categoria");
        await Actividades.ErrorAsync(await e.Jn(3).Cliente.PutAsync(Ruta(e, e.Jn(3)), Buenas), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(await e.E1.Cliente.PutAsync(Ruta(e, e.K1), Buenas), HttpStatusCode.NotFound, "no_encontrado");
        await _fabrica.Categorias.RetirarAsync(e.Jn(8).Integrante, e.P.Integrante);
        await Actividades.ErrorAsync(await e.P.Cliente.PutAsync(Ruta(e, e.Jn(8)), Buenas), HttpStatusCode.Conflict, "jugador_retirado");
    }

    [Fact]
    public async Task Al_cambiar_de_categoria_conserva_sus_estadisticas_y_las_cambia_quien_entrena_la_nueva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(Ruta(e, j3), Buenas));

        await _fabrica.Actividades.CambiarCategoriaAsync(j3.Integrante, e.C2015);

        Assert.Equal(73, (await Actividades.LeerAsync(await e.P.Cliente.GetAsync(RutaCarta(e, j3)))).GetProperty("valoracion").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await e.E1.Cliente.PutAsync(Ruta(e, j3), Buenas)).StatusCode);
        await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(Ruta(e, j3), Seis(80, 80, 80, 80, 80, 80)));
    }

    [Fact]
    public async Task Sin_asignacion_deja_de_verla_y_lo_que_puso_se_conserva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(Ruta(e, j3), Buenas));

        await _fabrica.ConContextoAsync(contexto => contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
            .Where(fila => fila.UsuarioRolId == e.E1.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Activa, false)));

        await Actividades.ErrorAsync(await e.E1.Cliente.GetAsync(RutaCarta(e, j3)), HttpStatusCode.NotFound, "no_encontrado");
        Assert.Equal(73, (await Actividades.LeerAsync(await e.P.Cliente.GetAsync(RutaCarta(e, j3)))).GetProperty("valoracion").GetInt32());
    }
}
