using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Lo que no deja agregar un hermano, y lo que pasa al confirmarlo dos veces (historia 1,
/// escenarios 5 a 7 y 10; RF-004 a RF-006 y RF-009; CE-002; supuestos 1, 2 y 4 del plan).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AgregarHermanoRechazosPruebas
{
    private const string Responsable = "Marta Gómez";

    private readonly FabricaApi _fabrica;

    public AgregarHermanoRechazosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static Dictionary<string, object?> Datos(string? numeroDocumento = null) =>
        EscenarioHermanos.DatosDeHermano(numeroDocumento, nombreResponsable: Responsable);

    // Escenario 1.6 y RF-006.
    [Fact]
    public async Task Un_dato_obligatorio_vacio_o_una_fecha_futura_responden_400_con_el_campo_y_nada_se_crea()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var invalidos = new Dictionary<string, object?>
        {
            ["nombres"] = " ",
            ["apellidos"] = null,
            ["tipoDocumento"] = null,
            ["numeroDocumento"] = "",
            ["fechaNacimiento"] = null,
        };

        foreach (var (campo, valor) in invalidos)
        {
            var cuerpo = Datos();
            cuerpo[campo] = valor;

            var respuesta = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);

            Assert.True(respuesta.StatusCode == HttpStatusCode.BadRequest, $"{campo}: {(int)respuesta.StatusCode}");
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.True((await respuesta.JsonAsync()).GetProperty("errores").TryGetProperty(campo, out _), campo);
        }

        var futura = Datos();
        futura["fechaNacimiento"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)).ToString("yyyy-MM-dd");
        var conFechaFutura = await familia.PostAsync(e.Hermanos(e.Ana.Id), futura);

        Assert.Equal(HttpStatusCode.BadRequest, conFechaFutura.StatusCode);
        Assert.True((await conFechaFutura.JsonAsync()).GetProperty("errores").TryGetProperty("fechaNacimiento", out _));
        Assert.Single(await e.JugadoresDeLaFamiliaAsync());
        Assert.Null((await e.FamiliaGuardadaAsync()).NombreResponsable);
    }

    // Escenario 1.5, RF-005 y CE-002: activo, retirado o en espera.
    [Fact]
    public async Task El_documento_de_otro_integrante_del_club_en_cualquier_estado_responde_409()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var (_, retirado) = await _fabrica.Categorias.CrearJugadorAsync(e.Club, 2014);
        await _fabrica.Categorias.RetirarAsync(retirado, e.Base.IntegrantePresidente);
        var (_, enEsperaDeOtraCuenta) = await _fabrica.Categorias.CrearJugadorEnEsperaAsync(e.Club, 2014);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var ocupados = new Dictionary<string, string>
        {
            ["activo"] = e.Beto.NumeroDocumento,
            ["el suyo"] = e.Ana.NumeroDocumento,
            ["presidente"] = e.Base.IntegrantePresidente.NumeroDocumento,
            ["retirado"] = retirado.NumeroDocumento,
            ["en espera de otra cuenta"] = enEsperaDeOtraCuenta.NumeroDocumento,
        };

        foreach (var (quien, numero) in ocupados)
        {
            // Con otra escritura del mismo número: se compara ya normalizado.
            var respuesta = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos($" {numero.ToUpperInvariant()} "));

            Assert.True(respuesta.StatusCode == HttpStatusCode.Conflict, $"{quien}: {(int)respuesta.StatusCode}");
            Assert.Equal("documento_repetido_en_club", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.Equal("Ese documento ya está registrado en el club.", await ClienteDePrueba.TituloAsync(respuesta));
        }

        Assert.Single(await e.JugadoresDeLaFamiliaAsync());
        Assert.Null((await e.FamiliaGuardadaAsync()).NombreResponsable);
    }

    // RF-034 y RF-039 de la 006 (cambia el supuesto 1) y §10.
    [Fact]
    public async Task El_documento_de_un_jugador_de_otra_cuenta_se_admite_el_de_un_presidente_no_y_el_de_la_misma_cuenta_si()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var (_, deOtraCuenta) = await _fabrica.Categorias.CrearJugadorAsync(e.OtroClub, 2014);
        var (_, presidente) = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, Rol.PRESIDENTE);
        var elMismoEnOtroClub = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Familia, Rol.JUGADOR);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        var ajeno = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos(deOtraCuenta.NumeroDocumento));
        var dePresidente = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos(presidente.NumeroDocumento));
        var propio = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos(elMismoEnOtroClub.NumeroDocumento));

        Assert.Equal(HttpStatusCode.Created, ajeno.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, dePresidente.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(dePresidente));
        Assert.Equal(HttpStatusCode.Created, propio.StatusCode);
        Assert.Equal(elMismoEnOtroClub.NumeroDocumento, (await e.JugadoresDeLaFamiliaAsync()).Last().NumeroDocumento);

        // En el otro club la cuenta sigue con su único integrante.
        Assert.Single(await e.JugadoresDeLaFamiliaAsync(e.OtroClub));
    }

    // Escenario 1.7 y RF-004.
    [Fact]
    public async Task Una_cuenta_sin_responsable_debe_indicarlo_para_un_hermano_menor_y_queda_en_la_cuenta()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        var sinResponsable = await familia.PostAsync(e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano());

        Assert.Equal(HttpStatusCode.BadRequest, sinResponsable.StatusCode);
        Assert.True((await sinResponsable.JsonAsync()).GetProperty("errores").TryGetProperty("nombreResponsable", out _));
        Assert.Single(await e.JugadoresDeLaFamiliaAsync());

        // A un hermano adulto no se le pide, y el que se escriba no se guarda.
        var adulto = EscenarioHermanos.DatosDeHermano(fechaNacimiento: new DateOnly(1990, 1, 1), nombreResponsable: "No se guarda");
        Assert.Equal(HttpStatusCode.Created, (await familia.PostAsync(e.Hermanos(e.Ana.Id), adulto)).StatusCode);
        Assert.Null((await e.FamiliaGuardadaAsync()).NombreResponsable);

        var conResponsable = await familia.PostAsync(
            e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano(nombreResponsable: $"  {Responsable}  "));

        Assert.Equal(HttpStatusCode.Created, conResponsable.StatusCode);
        Assert.Equal(Responsable, (await e.FamiliaGuardadaAsync()).NombreResponsable);
    }

    // Supuesto 4.
    [Fact]
    public async Task Si_la_cuenta_ya_tiene_responsable_se_conserva_el_que_tenia()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.Where(cuenta => cuenta.Id == e.Familia.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.NombreResponsable, Responsable)));
        var familia = await e.ClienteDeLaFamiliaAsync();

        var respuesta = await familia.PostAsync(
            e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano(nombreResponsable: "Otro Responsable"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(Responsable, (await e.FamiliaGuardadaAsync()).NombreResponsable);
    }

    // Escenario 1.10, RF-009 y supuesto 2.
    [Fact]
    public async Task Confirmar_dos_veces_el_mismo_hermano_deja_un_solo_jugador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var cuerpo = Datos();

        var primera = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);
        var segunda = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(
            (await primera.JsonAsync()).GetProperty("usuarioRolId").GetGuid(),
            (await segunda.JsonAsync()).GetProperty("usuarioRolId").GetGuid());
        Assert.Equal("EN_ESPERA", (await segunda.JsonAsync()).GetProperty("estadoIngreso").GetString());
        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);
    }

    [Fact]
    public async Task Dos_confirmaciones_a_la_vez_crean_un_solo_jugador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var uno = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var otro = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var cuerpo = Datos();

        var respuestas = await Task.WhenAll(
            uno.PostAsync(e.Hermanos(e.Ana.Id), cuerpo), otro.PostAsync(e.Hermanos(e.Ana.Id), cuerpo));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Created], respuestas.Select(respuesta => respuesta.StatusCode).Order());
        Assert.Equal(
            (await respuestas[0].JsonAsync()).GetProperty("usuarioRolId").GetGuid(),
            (await respuestas[1].JsonAsync()).GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);
    }
}
