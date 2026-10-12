using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Clubes;

/// <summary>
/// Una persona de varios clubes solo puede elegir entre sus clubes y solo ve los datos del club
/// elegido (constitución §20, §7.3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class VariosClubesPruebas
{
    private readonly FabricaApi _fabrica;

    public VariosClubesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_misma_sesion_ve_sus_dos_clubes_con_su_rol_y_recibe_404_en_un_tercero()
    {
        var primero = await _fabrica.Sembrador.CrearClubAsync();
        var segundo = await _fabrica.Sembrador.CrearClubAsync();
        var tercero = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var documento = Sembrador.Unico("doc");
        await _fabrica.Sembrador.CrearIntegranteAsync(primero, usuario, Rol.PRESIDENTE, documento);
        await _fabrica.Sembrador.CrearIntegranteAsync(segundo, usuario, Rol.JUGADOR, documento);
        await _fabrica.Sembrador.CrearIntegranteAsync(tercero, Rol.PRESIDENTE);
        var cliente = _fabrica.CrearClienteDePrueba();
        await cliente.IniciarSesionAsync(documento);

        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var enPrimero = await cliente.GetAsync($"/api/clubes/{primero.Id}");
        var enSegundo = await cliente.GetAsync($"/api/clubes/{segundo.Id}");
        var enTercero = await cliente.GetAsync($"/api/clubes/{tercero.Id}");

        var clubes = sesion.GetProperty("clubes").EnumerateArray()
            .ToDictionary(c => c.GetProperty("clubId").GetGuid(), c => c.GetProperty("rol").GetString());
        Assert.Equal(2, clubes.Count);
        Assert.Equal("PRESIDENTE", clubes[primero.Id]);
        Assert.Equal("JUGADOR", clubes[segundo.Id]);
        Assert.False(clubes.ContainsKey(tercero.Id));

        Assert.Equal("PRESIDENTE", (await ClienteDePrueba.LeerAsync<JsonElement>(enPrimero)).GetProperty("miRol").GetString());
        var dtoSegundo = await ClienteDePrueba.LeerAsync<JsonElement>(enSegundo);
        Assert.Equal("JUGADOR", dtoSegundo.GetProperty("miRol").GetString());
        Assert.Equal(segundo.Nombre, dtoSegundo.GetProperty("nombre").GetString());
        Assert.Equal(HttpStatusCode.NotFound, enTercero.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(enTercero));
    }

    [Fact]
    public async Task El_mismo_documento_puede_existir_en_dos_clubes_bajo_la_misma_cuenta()
    {
        var primero = await _fabrica.Sembrador.CrearClubAsync();
        var segundo = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var documento = Sembrador.Unico("doc");

        var uno = await _fabrica.Sembrador.CrearIntegranteAsync(primero, usuario, Rol.ENTRENADOR, documento);
        var dos = await _fabrica.Sembrador.CrearIntegranteAsync(segundo, usuario, Rol.ENTRENADOR, documento);

        Assert.Equal(uno.NumeroDocumento, dos.NumeroDocumento);
        Assert.NotEqual(uno.ClubId, dos.ClubId);
    }
}
