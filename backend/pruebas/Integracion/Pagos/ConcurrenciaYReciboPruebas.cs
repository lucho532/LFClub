using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// Dos confirmaciones a la vez sobre el mismo jugador no cubren más de lo que vale el cargo, y el recibo
/// es consecutivo por club (spec 015, historia 4; research §7; RF-025).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConcurrenciaYReciboPruebas
{
    private readonly FabricaApi _fabrica;

    public ConcurrenciaYReciboPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private object Cuerpo(decimal valor) => new
    {
        valor,
        medio = "EFECTIVO",
        fecha = _fabrica.Reloj.Hoy.ToString("yyyy-MM-dd"),
    };

    [Fact]
    public async Task Dos_pagos_simultaneos_por_todo_el_cargo_dejan_uno_aplicado_y_el_otro_rechazado()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var primero = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        var segundo = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        var ruta = c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos");

        var respuestas = await Task.WhenAll(
            primero.PostAsync(ruta, Cuerpo(65000m)), segundo.PostAsync(ruta, Cuerpo(65000m)));

        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Created);
        var rechazada = Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains(await ClienteDePrueba.CodigoAsync(rechazada), new[] { "sin_deuda", "pago_excede_deuda" });
        Assert.Equal(65000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task Varios_pagos_parciales_simultaneos_nunca_superan_el_valor_del_cargo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var cargo = await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var ruta = c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos");
        var clientes = new List<ClienteDePrueba>();
        for (var i = 0; i < 6; i++)
        {
            clientes.Add(await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario));
        }

        var respuestas = await Task.WhenAll(clientes.Select(cliente => cliente.PostAsync(ruta, Cuerpo(20000m))));

        // Caben tres de 20.000 en 65.000; el cuarto ya excede los 5.000 que quedan.
        Assert.Equal(3, respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(60000m, await _fabrica.Pagos.PagadoAsync(cargo));
    }

    [Fact]
    public async Task El_recibo_es_consecutivo_en_el_club_y_dos_clubes_pueden_repetir_numero()
    {
        var uno = await _fabrica.Pagos.CrearClubAsync();
        var otro = await _fabrica.Pagos.CrearClubAsync();
        foreach (var club in new[] { uno, otro })
        {
            await _fabrica.Pagos.CrearCargoAsync(club.Ana.Integrante, new DateOnly(2026, 8, 1));
            await _fabrica.Pagos.CrearCargoAsync(club.Ana.Integrante, new DateOnly(2026, 9, 1));
            await _fabrica.Pagos.CrearCargoAsync(club.Ana.Integrante, new DateOnly(2026, 10, 1));
        }

        async Task<List<int>> RecibosAsync(ClubDePagos club)
        {
            var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(club.Presidente.Usuario);
            var numeros = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                var respuesta = await presidente.PostAsync(club.Ruta($"/jugadores/{club.Ana.Integrante.Id}/pagos"), Cuerpo(65000m));
                numeros.Add((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("numeroRecibo").GetInt32());
            }

            return numeros;
        }

        Assert.Equal([1, 2, 3], await RecibosAsync(uno));
        Assert.Equal([1, 2, 3], await RecibosAsync(otro));
    }

    [Fact]
    public async Task Un_pago_rechazado_por_exceso_no_consume_un_numero_de_recibo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.Pagos.CrearCargoAsync(c.Ana.Integrante, new DateOnly(2026, 10, 1));
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        var ruta = c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/pagos");

        var excede = await presidente.PostAsync(ruta, Cuerpo(100000m));
        var bien = await presidente.PostAsync(ruta, Cuerpo(65000m));

        Assert.Equal(HttpStatusCode.Conflict, excede.StatusCode);
        Assert.Equal(1, (await ClienteDePrueba.LeerAsync<JsonElement>(bien)).GetProperty("numeroRecibo").GetInt32());
    }
}
