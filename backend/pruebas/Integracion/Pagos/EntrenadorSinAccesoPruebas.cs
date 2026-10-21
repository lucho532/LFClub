using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Aislamiento;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// El ENTRENADOR queda fuera de todo lo financiero: las 18 operaciones nuevas le responden 403, también
/// sobre los jugadores de la categoría que tiene asignada, y ninguna respuesta que ve lleva datos de pago
/// (spec 015; RF-035, RF-037; CE-007; constitución §16.9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EntrenadorSinAccesoPruebas
{
    private readonly FabricaApi _fabrica;

    public EntrenadorSinAccesoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Las operaciones que añade esta funcionalidad, reconocidas por su ruta.</summary>
    private List<Endpoint> Nuevas => EndpointsDeLaApi.DeLaApi(_fabrica)
        .Where(endpoint => endpoint.Ruta.StartsWith("/api/clubes/{clubId}", StringComparison.Ordinal))
        .Where(endpoint => endpoint.Ruta.Contains("/mensualidad", StringComparison.Ordinal)
            || endpoint.Ruta.Contains("/medios-de-pago", StringComparison.Ordinal)
            || endpoint.Ruta.Contains("/finanzas", StringComparison.Ordinal)
            || endpoint.Ruta.Contains("/pagos", StringComparison.Ordinal)
            || endpoint.Ruta.EndsWith("/cuenta", StringComparison.Ordinal))
        .ToList();

    [Fact]
    public void Son_exactamente_las_18_operaciones_nuevas_del_contrato()
    {
        Assert.Equal(18, Nuevas.Count);
    }

    [Fact]
    public async Task Las_18_operaciones_responden_403_al_entrenador_aunque_entrene_la_categoria_del_jugador()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var entrenador = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Entrenador.Usuario);

        foreach (var endpoint in Nuevas)
        {
            var respuesta = await AccesoPlataformaPruebas.EnviarAsync(entrenador, endpoint, c.Club.Id);

            Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{endpoint}: {(int)respuesta.StatusCode}");
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }

    [Fact]
    public async Task Las_operaciones_sobre_los_jugadores_y_pagos_de_su_categoria_tambien_responden_403()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var pago = await _fabrica.Pagos.CrearPagoConfirmadoAsync(c.Ana.Integrante, c.Presidente.Integrante, 65000m, cargo);
        var entrenador = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Entrenador.Usuario);

        var respuestas = new[]
        {
            await entrenador.GetAsync(c.Cuenta(c.Ana.Integrante)),
            await entrenador.GetAsync(c.Ruta($"/pagos/{pago.Id}")),
            await entrenador.GetAsync(c.Ruta($"/pagos/{pago.Id}/resguardo")),
            await entrenador.GetAsync(c.Ruta($"/pagos/{pago.Id}/soporte")),
            await entrenador.GetAsync(c.Ruta("/pagos/en-revision")),
            await entrenador.GetAsync(c.Ruta("/finanzas")),
            await entrenador.GetAsync(c.Ruta("/mensualidad")),
            await entrenador.GetAsync(c.Ruta("/medios-de-pago")),
            await entrenador.PostAsync(c.Ruta($"/pagos/{pago.Id}/anulacion"), new { motivo = "x" }),
        };

        Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode));
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task La_lista_de_jugadores_le_llega_sin_estado_de_cuenta_y_ninguna_respuesta_lleva_datos_financieros()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var entrenador = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Entrenador.Usuario);

        var lista = await entrenador.GetAsync(c.Ruta("/jugadores"));
        var texto = await lista.Content.ReadAsStringAsync();
        var jugadores = (await ClienteDePrueba.LeerAsync<JsonElement>(lista)).EnumerateArray()
            .SelectMany(grupo => grupo.GetProperty("jugadores").EnumerateArray()).ToList();

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.NotEmpty(jugadores);
        Assert.All(jugadores, jugador =>
            Assert.True(!jugador.TryGetProperty("estadoCuenta", out var estado) || estado.ValueKind == JsonValueKind.Null));
        foreach (var palabra in new[] { "saldo", "DEBE", "AL_DIA", "PAGO_PARCIAL", "mensualidad", "recaudo" })
        {
            Assert.DoesNotContain(palabra, texto, StringComparison.OrdinalIgnoreCase);
        }

        // Su propio inicio y las fichas de su categoría tampoco traen nada de pagos.
        var ficha = await entrenador.GetAsync(c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/ficha"));
        var cuerpoFicha = await ficha.Content.ReadAsStringAsync();
        Assert.DoesNotContain("saldo", cuerpoFicha, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("estadoCuenta", cuerpoFicha, StringComparison.OrdinalIgnoreCase);
    }
}
