using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// Una persona que es jugador en dos clubes tiene una ficha en cada uno: la salud, el contacto de
/// emergencia y los archivos no cruzan de club; el celular y el responsable, que son de la cuenta,
/// sí (RF-003, RF-039; supuesto 4 del plan).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaEntreClubesPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;

    private readonly FabricaApi _fabrica;

    public FichaEntreClubesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Ana, además de en su club, como jugadora del otro club con la misma cuenta.</summary>
    private async Task<(EscenarioFicha E, UsuarioRol AnaEnElOtro, string FichaEnElOtro)> ConAnaEnDosClubesAsync()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var cuenta = (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!;
        var enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(e.Base.OtroClub, cuenta, Rol.JUGADOR);
        return (e, enElOtro, EscenarioFicha.Ficha(e.Base.OtroClub.Id, enElOtro.Id));
    }

    // RF-003.
    [Fact]
    public async Task Lo_guardado_de_salud_emergencia_y_archivos_en_un_club_no_aparece_en_el_otro()
    {
        var (e, anaEnElOtro, fichaEnElOtro) = await ConAnaEnDosClubesAsync();
        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());
        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), SembradorFichas.Pdf());

        // Ni para la familia ni para el presidente del otro club.
        foreach (var cliente in new[] { e.CuentaDeAna, e.Base.PresidenteDeOtroClub })
        {
            var respuesta = await cliente.GetAsync(fichaEnElOtro);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var ficha = await respuesta.JsonAsync();
            Assert.All(ficha.GetProperty("contactoEmergencia").EnumerateObject(), dato => Assert.Equal(System.Text.Json.JsonValueKind.Null, dato.Value.ValueKind));
            Assert.All(ficha.GetProperty("seguridadSocial").EnumerateObject(), dato => Assert.Equal(System.Text.Json.JsonValueKind.Null, dato.Value.ValueKind));
            Assert.All(ficha.GetProperty("datosClinicos").EnumerateObject(), dato => Assert.Equal(System.Text.Json.JsonValueKind.Null, dato.Value.ValueKind));
            Assert.All(ficha.Lista("documentos"), documento => Assert.False(documento.GetProperty("entregado").GetBoolean()));
            Assert.False(ficha.Tiene("ultimoCambio"));
            Assert.DoesNotContain("Polen", await respuesta.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"{fichaEnElOtro}/documentos/{Copia}")).StatusCode);
        }

        Assert.Null(await e.FichaGuardadaAsync(anaEnElOtro.Id));
        Assert.Empty(await e.DocumentosGuardadosAsync(anaEnElOtro.Id));
    }

    [Fact]
    public async Task Cada_club_guarda_su_propia_ficha_sin_tocar_la_del_otro()
    {
        var (e, anaEnElOtro, fichaEnElOtro) = await ConAnaEnDosClubesAsync();
        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());
        var enElOtro = EscenarioFicha.Cuerpo();
        enElOtro["alergias"] = "Lactosa";
        enElOtro["entidadSalud"] = "Otra EPS";

        var respuesta = await e.CuentaDeAna.PutAsync(fichaEnElOtro, enElOtro);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Polen", (await e.FichaGuardadaAsync(e.Ana.Id))!.Alergias);
        Assert.Equal("Nueva EPS", (await e.FichaGuardadaAsync(e.Ana.Id))!.EntidadSalud);
        Assert.Equal("Lactosa", (await e.FichaGuardadaAsync(anaEnElOtro.Id))!.Alergias);
        Assert.Equal("Otra EPS", (await e.FichaGuardadaAsync(anaEnElOtro.Id))!.EntidadSalud);
    }

    // RF-039: es el único dato de la ficha que un club cambia con efecto en otro.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task El_celular_y_el_responsable_cambiados_en_un_club_se_leen_en_el_otro(bool loCambiaElPresidente)
    {
        var (e, _, fichaEnElOtro) = await ConAnaEnDosClubesAsync();
        var quien = loCambiaElPresidente ? e.Presidente : e.CuentaDeAna;

        var respuesta = await quien.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo("3175550099", "Rosa Marín"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach (var cliente in new[] { e.CuentaDeAna, e.Base.PresidenteDeOtroClub })
        {
            var contacto = (await (await cliente.GetAsync(fichaEnElOtro)).JsonAsync()).GetProperty("contacto");
            Assert.Equal("3175550099", contacto.GetProperty("celular").GetString());
            Assert.Equal("Rosa Marín", contacto.GetProperty("nombreResponsable").GetString());
        }
    }

    // Supuesto 4: el sello se pone solo en la ficha desde la que se hizo el cambio.
    [Fact]
    public async Task Ese_cambio_sella_el_ultimo_cambio_solo_en_la_ficha_desde_la_que_se_hizo()
    {
        var (e, anaEnElOtro, fichaEnElOtro) = await ConAnaEnDosClubesAsync();
        await _fabrica.Fichas.CrearFichaAsync(anaEnElOtro, cambiadaEn: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var selloDelOtro = (await e.FichaGuardadaAsync(anaEnElOtro.Id))!.UltimoCambioEn;

        await e.Presidente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo("3175550099", "Rosa Marín"));

        Assert.NotNull(await e.FichaGuardadaAsync(e.Ana.Id));
        Assert.Equal(selloDelOtro, (await e.FichaGuardadaAsync(anaEnElOtro.Id))!.UltimoCambioEn);
        var enElOtro = await (await e.CuentaDeAna.GetAsync(fichaEnElOtro)).JsonAsync();
        Assert.Equal("3175550099", enElOtro.GetProperty("contacto").GetProperty("celular").GetString());
        Assert.True(enElOtro.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());
    }

    // El presidente de un club no llega a la cuenta por un jugador que no es de su club.
    [Fact]
    public async Task El_presidente_de_un_club_no_cambia_el_celular_por_la_ficha_del_otro_club()
    {
        var (e, anaEnElOtro, _) = await ConAnaEnDosClubesAsync();

        var respuesta = await e.Presidente.PutAsync(e.Ficha(anaEnElOtro.Id), EscenarioFicha.Cuerpo("3175550099"));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("3001234567", (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!.Celular);
    }
}
