using System.Net;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// La ficha muestra quién la cambió por última vez y cuándo, y solo eso (historia 1, escenarios 9
/// y 10; RF-038; CE-012; supuestos 3 y 4 del plan).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class UltimoCambioPruebas
{
    private readonly FabricaApi _fabrica;

    public UltimoCambioPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Antes_del_primer_cambio_no_existe_la_propiedad_ni_la_fila()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        Assert.False((await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).Tiene("ultimoCambio"));
        Assert.False((await e.FichaDeAsync(e.Presidente, e.Ana.Id)).Tiene("ultimoCambio"));
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    [Fact]
    public async Task Tras_guardar_la_familia_trae_la_fecha_su_nombre_y_que_fue_la_cuenta_del_jugador()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());

        // Lo ve todo el que puede ver la ficha.
        foreach (var ficha in new[] { await respuesta.JsonAsync(), await e.FichaDeAsync(e.Directivo, e.Ana.Id), await e.FichaDeAsync(e.Entrenador, e.Ana.Id) })
        {
            var cambio = ficha.GetProperty("ultimoCambio");
            Assert.InRange(cambio.GetProperty("fecha").GetDateTime(), antes, DateTime.UtcNow.AddSeconds(1));
            Assert.Equal($"{e.Ana.Nombres} {e.Ana.Apellidos}", cambio.GetProperty("autor").GetString());
            Assert.True(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
        }
    }

    // CE-012: el segundo cambio sustituye al primero; no hay historial.
    [Fact]
    public async Task Un_segundo_cambio_sustituye_al_primero()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana, e.Base.IntegrantePresidente, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.False((await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());

        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());

        var cambio = (await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).GetProperty("ultimoCambio");
        Assert.True(cambio.GetProperty("fecha").GetDateTime() > new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        Assert.True(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
        Assert.Equal(1, await FilasDeAsync(e.Ana.Id));
    }

    [Fact]
    public async Task Guardar_lo_mismo_dos_veces_no_mueve_la_fecha()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());
        var primera = (await e.FichaGuardadaAsync(e.Ana.Id))!.UltimoCambioEn;

        var respuesta = await e.Presidente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardada = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        Assert.Equal(primera, guardada.UltimoCambioEn);
        Assert.Equal(e.Ana.UsuarioId, guardada.UltimoCambioPorUsuarioId);
    }

    // Research §9: sin ningún dato distinto no se crea la fila.
    [Fact]
    public async Task Guardar_sin_cambiar_nada_en_una_ficha_vacia_no_crea_la_fila()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuenta, adulto) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 1995);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PutAsync(e.Ficha(adulto.Id), new { celular = cuenta.Celular });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False((await respuesta.JsonAsync()).Tiene("ultimoCambio"));
        Assert.Null(await e.FichaGuardadaAsync(adulto.Id));
    }

    [Fact]
    public async Task Cambiar_solo_el_celular_tambien_sella_el_ultimo_cambio()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuenta, adulto) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 1995);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PutAsync(e.Ficha(adulto.Id), new { celular = "3187776655" });

        Assert.True((await respuesta.JsonAsync()).Tiene("ultimoCambio"));
        Assert.Equal(cuenta.Id, (await e.FichaGuardadaAsync(adulto.Id))!.UltimoCambioPorUsuarioId);
    }

    // Caso límite: queda el último guardado entero, sin mezclar datos de los dos (research §10).
    [Fact]
    public async Task Dos_guardados_simultaneos_dejan_una_sola_fila_que_coincide_con_uno_de_los_dos()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        for (var vuelta = 0; vuelta < 5; vuelta++)
        {
            var (cuenta, jugador) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 2014);
            var familia = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);
            var deLaFamilia = Datos("3100000001", "Responsable Uno", "Uno");
            var delPresidente = Datos("3200000002", "Responsable Dos", "Dos");

            var respuestas = await Task.WhenAll(
                familia.PutAsync(e.Ficha(jugador.Id), deLaFamilia),
                e.Presidente.PutAsync(e.Ficha(jugador.Id), delPresidente));

            Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
            Assert.Equal(1, await FilasDeAsync(jugador.Id));
            var ficha = (await e.FichaGuardadaAsync(jugador.Id))!;
            var guardado = await e.JugadorGuardadoAsync(jugador.Id);
            var quedo = new[]
            {
                guardado.Usuario!.Celular, guardado.Usuario.NombreResponsable, ficha.EmergenciaNombre, ficha.EmergenciaParentesco,
                ficha.EmergenciaCelular, ficha.EntidadSalud, ficha.LugarAtencion, ficha.Alergias, ficha.Enfermedades,
                ficha.Medicamentos, ficha.Observaciones,
            };
            Assert.Contains(quedo, new[] { Textos(deLaFamilia), Textos(delPresidente) });

            // El sello es de quien guardó el último.
            var ganoLaFamilia = quedo[0] == "3100000001";
            Assert.Equal(ganoLaFamilia ? cuenta.Id : e.Base.IntegrantePresidente.UsuarioId, ficha.UltimoCambioPorUsuarioId);
        }
    }

    /// <summary>Un formulario completo en el que todos los textos llevan la misma marca.</summary>
    private static Dictionary<string, object?> Datos(string celular, string responsable, string marca) => new()
    {
        ["celular"] = celular,
        ["nombreResponsable"] = responsable,
        ["emergenciaNombre"] = $"Emergencia {marca}",
        ["emergenciaParentesco"] = $"Parentesco {marca}",
        ["emergenciaCelular"] = celular,
        ["entidadSalud"] = $"Entidad {marca}",
        ["lugarAtencion"] = $"Lugar {marca}",
        ["alergias"] = $"Alergias {marca}",
        ["enfermedades"] = $"Enfermedades {marca}",
        ["medicamentos"] = $"Medicamentos {marca}",
        ["observaciones"] = $"Observaciones {marca}",
    };

    private static string?[] Textos(Dictionary<string, object?> datos) => datos.Values.Select(valor => (string?)valor).ToArray();

    private Task<int> FilasDeAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.FichasJugador.IgnoreQueryFilters().CountAsync(ficha => ficha.UsuarioRolId == usuarioRolId));
}
