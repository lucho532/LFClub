using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Ingresos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// La sala de espera le dice al PRESIDENTE de qué jugador del club es hermano cada ingreso
/// (historia 3, escenario 1; RF-015; supuesto 5 del plan).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class HermanoDePruebas
{
    private readonly FabricaApi _fabrica;

    public HermanoDePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static async Task<JsonElement> IngresoAsync(EscenarioHermanos e, Guid usuarioRolId) =>
        (await e.Presidente.ListaAsync(EscenarioIngresos.EnEspera(e.Club)))
            .Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == usuarioRolId);

    // Escenario 3.1.
    [Fact]
    public async Task Un_hermano_en_espera_llega_con_sus_datos_el_contacto_de_la_cuenta_y_de_quien_es_hermano()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var cuerpo = EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez");
        var agregado = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);
        Assert.Equal(HttpStatusCode.Created, agregado.StatusCode);
        var hermanoId = (await ClienteDePrueba.LeerAsync<JsonElement>(agregado)).GetProperty("usuarioRolId").GetGuid();

        var ingreso = await IngresoAsync(e, hermanoId);

        Assert.Equal("Luis", ingreso.GetProperty("nombres").GetString());
        Assert.Equal("Gómez", ingreso.GetProperty("apellidos").GetString());
        Assert.Equal("TARJETA_IDENTIDAD", ingreso.GetProperty("tipoDocumento").GetString());
        Assert.Equal(cuerpo["numeroDocumento"], ingreso.GetProperty("numeroDocumento").GetString());
        Assert.Equal(cuerpo["fechaNacimiento"], ingreso.GetProperty("fechaNacimiento").GetString());
        Assert.Equal(e.Familia.Correo, ingreso.GetProperty("correo").GetString());
        Assert.Equal("3001234567", ingreso.GetProperty("celular").GetString());
        Assert.Equal("Marta Gómez", ingreso.GetProperty("nombreResponsable").GetString());
        Assert.Equal($"{e.Ana.Nombres} {e.Ana.Apellidos}", ingreso.GetProperty("hermanoDe").GetString());
    }

    // Es el de la ficha desde la que se agregó, no cualquier otro jugador de la cuenta.
    [Fact]
    public async Task Con_tres_jugadores_en_la_cuenta_dice_el_de_la_ficha_desde_la_que_se_agrego()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var mara = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, nombres: "Mara");
        var conMara = await e.ClienteDeLaFamiliaAsync(mara.Id);

        var agregado = await conMara.PostAsync(e.Hermanos(mara.Id), EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez"));

        Assert.Equal(HttpStatusCode.Created, agregado.StatusCode);
        var hermanoId = (await ClienteDePrueba.LeerAsync<JsonElement>(agregado)).GetProperty("usuarioRolId").GetGuid();
        Assert.Equal($"Mara {mara.Apellidos}", (await IngresoAsync(e, hermanoId)).GetProperty("hermanoDe").GetString());
        Assert.Equal(mara.Id, (await _fabrica.IntegranteAsync(hermanoId))!.AgregadoDesdeUsuarioRolId);
    }

    // Supuesto 5: borrar al jugador de origen no borra a su hermano.
    [Fact]
    public async Task Si_el_jugador_de_origen_deja_de_existir_el_hermano_sigue_en_espera_sin_decir_de_quien_lo_es()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);

        await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == e.Ana.Id).ExecuteDeleteAsync());

        var ingreso = await IngresoAsync(e, hermano.Id);
        Assert.Equal(JsonValueKind.Null, ingreso.GetProperty("hermanoDe").ValueKind);
        var guardado = (await _fabrica.IntegranteAsync(hermano.Id))!;
        Assert.Null(guardado.AgregadoDesdeUsuarioRolId);
        Assert.Equal(EstadoIngreso.EN_ESPERA, guardado.EstadoIngreso);
    }

    [Fact]
    public async Task Un_ingreso_en_espera_sin_origen_llega_sin_hermano()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var (_, sinOrigen) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(e.Club);

        var ingreso = await IngresoAsync(e, sinOrigen.Id);

        Assert.True(ingreso.TryGetProperty("hermanoDe", out var hermanoDe));
        Assert.Equal(JsonValueKind.Null, hermanoDe.ValueKind);
    }
}
