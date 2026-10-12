using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LaPecosa.Pruebas.Integracion.Carta.ConsultaCartaPruebas;

namespace LaPecosa.Pruebas.Integracion.Carta;

/// <summary>
/// Dorsal y posición: los cambian la familia, quien entrena la categoría y el PRESIDENTE; vale el
/// último cambio (spec 010, escenario 3.12; RF-027; supuesto 2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DorsalYPosicionPruebas
{
    private readonly FabricaApi _fabrica;

    public DorsalYPosicionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Ruta(EscenarioActividades e, Persona jugador) => $"{RutaCarta(e, jugador)}/dorsal-y-posicion";

    private static object Datos(object? dorsal, string? posicion) => new { dorsal, posicion };

    [Fact]
    public async Task La_familia_pone_dorsal_y_posicion_y_vale_el_ultimo_cambio()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);

        var deLaFamilia = await Actividades.LeerAsync(await j3.Cliente.PutAsync(Ruta(e, j3), Datos(10, "DELANTERO")));
        Assert.Equal(10, deLaFamilia.GetProperty("dorsal").GetInt32());
        Assert.Equal("DELANTERO", deLaFamilia.GetProperty("posicion").GetString());

        var deE2 = await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(Ruta(e, j3), Datos(7, "DELANTERO")));
        Assert.Equal(7, deE2.GetProperty("dorsal").GetInt32());

        // El dorsal puede repetirse entre jugadores.
        await Actividades.LeerAsync(await e.Jn(5).Cliente.PutAsync(Ruta(e, e.Jn(5)), Datos(7, null)));
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(Ruta(e, j3), Datos(9, "PORTERO")));
        var deD1 = await Actividades.LeerAsync(await e.D1.Cliente.PutAsync(Ruta(e, j3), Datos(11, null)));
        Assert.False(deD1.TryGetProperty("posicion", out _));
        Assert.Equal(7, (await Actividades.LeerAsync(await e.P.Cliente.GetAsync(RutaCarta(e, e.Jn(5))))).GetProperty("dorsal").GetInt32());
    }

    [Fact]
    public async Task Quien_no_puede_cambiarlos_recibe_403_o_404()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        await Actividades.ErrorAsync(
            await e.D2.Cliente.PutAsync(Ruta(e, e.Jn(3)), Datos(10, null)), HttpStatusCode.Forbidden, "no_entrena_categoria");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(Ruta(e, e.K1), Datos(10, null)), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.Jn(4).Cliente.PutAsync(Ruta(e, e.Jn(3)), Datos(10, null)), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(100, null)]
    [InlineData(null, "LIBERO")]
    [InlineData(10.5, null)]
    public async Task Valores_fuera_de_lo_admitido_responden_400(object? dorsal, string? posicion)
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PutAsync(Ruta(e, e.Jn(3)), Datos(dorsal, posicion)), HttpStatusCode.BadRequest, "datos_invalidos");
    }

    [Fact]
    public async Task La_de_un_retirado_no_se_cambia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Categorias.RetirarAsync(e.Jn(8).Integrante, e.P.Integrante);

        await Actividades.ErrorAsync(
            await e.P.Cliente.PutAsync(Ruta(e, e.Jn(8)), Datos(10, null)), HttpStatusCode.Conflict, "jugador_retirado");
    }

    [Fact]
    public async Task Guardar_lo_mismo_no_crea_la_carta_ni_mueve_el_sello()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        await Actividades.LeerAsync(await j3.Cliente.PutAsync(Ruta(e, j3), Datos(null, null)));
        Assert.Null(await SelloAsync(j3));

        await Actividades.LeerAsync(await j3.Cliente.PutAsync(Ruta(e, j3), Datos(10, null)));
        var primero = await SelloAsync(j3);
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(Ruta(e, j3), Datos(10, null)));

        Assert.NotNull(primero);
        Assert.Equal(primero, await SelloAsync(j3));
    }

    private Task<(DateTime?, string?)?> SelloAsync(Persona jugador) => _fabrica.ConContextoAsync(async contexto =>
    {
        var carta = await contexto.CartasJugador.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(fila => fila.UsuarioRolId == jugador.Id);
        return carta is null ? ((DateTime?, string?)?)null : (carta.DorsalCambiadoEn, carta.DorsalCambiadoPorNombre);
    });
}
