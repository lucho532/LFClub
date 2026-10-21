using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// La columna "Al día" de la lista de jugadores deja de ser un espacio reservado: trae el estado real
/// para el PRESIDENTE y el DIRECTIVO, y va nula para el ENTRENADOR (spec 015, historia 3; RF-034, RF-037).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ListaConEstadoDeCuentaPruebas
{
    private readonly FabricaApi _fabrica;

    public ListaConEstadoDeCuentaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<Dictionary<Guid, JsonElement>> ListaAsync(Usuario quien, ClubDePagos c)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(quien);
        var respuesta = await cliente.GetAsync(c.Ruta("/jugadores"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).EnumerateArray()
            .SelectMany(grupo => grupo.GetProperty("jugadores").EnumerateArray())
            .ToDictionary(jugador => jugador.GetProperty("usuarioRolId").GetGuid());
    }

    private async Task<(ClubDePagos, UsuarioRol)> ClubConTresEstadosAsync()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var caro = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.Categoria, "Caro");
        var cargoAna = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var cargoCaro = await _fabrica.Pagos.CrearCargoAsync(caro.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Pagos.CrearCargoAsync(c.Beto.Integrante, new DateOnly(2026, 10, 1));
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargoAna);
        await _fabrica.Pagos.CrearPagoConfirmadoAsync(caro.Integrante, c.Presidente.Integrante, 20000m, cargoCaro);
        return (c, caro.Integrante);
    }

    [Fact]
    public async Task El_presidente_y_el_directivo_ven_al_dia_pago_parcial_y_debe()
    {
        var (c, caro) = await ClubConTresEstadosAsync();

        foreach (var quien in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var lista = await ListaAsync(quien, c);

            Assert.Equal("AL_DIA", lista[c.Ana.Integrante.Id].GetProperty("estadoCuenta").GetString());
            Assert.Equal("PAGO_PARCIAL", lista[caro.Id].GetProperty("estadoCuenta").GetString());
            Assert.Equal("DEBE", lista[c.Beto.Integrante.Id].GetProperty("estadoCuenta").GetString());
        }
    }

    [Fact]
    public async Task Un_jugador_sin_cargos_aparece_al_dia()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var lista = await ListaAsync(c.Presidente.Usuario, c);

        Assert.All(lista.Values, jugador => Assert.Equal("AL_DIA", jugador.GetProperty("estadoCuenta").GetString()));
    }

    [Fact]
    public async Task Al_entrenador_el_estado_de_cuenta_le_llega_nulo()
    {
        var (c, _) = await ClubConTresEstadosAsync();

        var lista = await ListaAsync(c.Entrenador.Usuario, c);

        Assert.NotEmpty(lista);
        Assert.All(lista.Values, jugador =>
        {
            Assert.True(
                !jugador.TryGetProperty("estadoCuenta", out var estado) || estado.ValueKind == JsonValueKind.Null);
        });
    }
}
