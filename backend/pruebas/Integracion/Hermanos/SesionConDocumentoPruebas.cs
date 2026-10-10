using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using LaPecosa.Pruebas.Integracion.Ficha;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Entrar con el documento de un jugador deja la sesión limitada a él: no recibe ni alcanza nada
/// de sus hermanos, tampoco tras renovar el token o cambiar su documento (historia 2, escenarios 5
/// y 6; RF-026; CE-007).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class SesionConDocumentoPruebas
{
    private readonly FabricaApi _fabrica;

    public SesionConDocumentoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    /// <summary>Entra por la API con ese identificador y la contraseña de la cuenta.</summary>
    private async Task<ClienteDePrueba> EntrarAsync(string identificador)
    {
        var cliente = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(identificador)).StatusCode);
        return cliente;
    }

    private static async Task<List<JsonElement>> ClubesAsync(ClienteDePrueba cliente) =>
        (await (await cliente.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes");

    // Escenario 2.5 y CE-007. Luis es el más reciente: sin limitación la sesión describiría a Ana.
    [Fact]
    public async Task Entrar_con_el_documento_de_un_jugador_devuelve_solo_a_ese_jugador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var cliente = await EntrarAsync(luis.NumeroDocumento);

        var respuesta = await cliente.GetAsync("/api/sesion");

        var club = (await respuesta.JsonAsync()).Lista("clubes").Single();
        Assert.Equal(e.Club.Id, club.GetProperty("clubId").GetGuid());
        Assert.Empty(club.Lista("jugadores"));
        Assert.Equal(luis.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal("Luis", club.GetProperty("nombres").GetString());
        Assert.Equal("APROBADO", club.GetProperty("estadoIngreso").GetString());
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"Ana\"", cuerpo);
        Assert.DoesNotContain(e.Ana.Id.ToString(), cuerpo);
    }

    // Escenario 2.6 y RF-026.
    [Fact]
    public async Task La_sesion_limitada_hace_las_peticiones_como_ese_jugador_y_no_llega_al_hermano()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");
        var cliente = await EntrarAsync(luis.NumeroDocumento);
        var fichaDeAna = EscenarioFicha.Ficha(e.Club.Id, e.Ana.Id);

        var sinCabecera = await cliente.GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.OK, sinCabecera.StatusCode);
        Assert.Equal(luis.Id, (await sinCabecera.JsonAsync()).GetProperty("miUsuarioRolId").GetGuid());

        foreach (var elegido in new Guid?[] { null, e.Ana.Id, luis.Id })
        {
            cliente.ElegirJugador(elegido);
            HttpResponseMessage[] delHermano =
            [
                await cliente.GetAsync(fichaDeAna),
                await cliente.PutAsync(fichaDeAna, EscenarioFicha.Cuerpo()),
                await cliente.GetAsync($"{fichaDeAna}/documentos/{DocumentoPedido.CERTIFICADO_SALUD}"),
                await cliente.SubirAsync($"{fichaDeAna}/documentos/{DocumentoPedido.CERTIFICADO_SALUD}", SembradorFichas.Pdf()),
            ];

            foreach (var respuesta in delHermano)
            {
                Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{elegido}: {(int)respuesta.StatusCode}");
                Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        // Con la cabecera del hermano ni siquiera entra al club.
        var conLaDelHermano = await cliente.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.NotFound, conLaDelHermano.StatusCode);
        Assert.Equal("3001234567", (await e.FamiliaGuardadaAsync()).Celular);
    }

    [Fact]
    public async Task La_limitacion_sigue_tras_renovar_el_token_y_tras_cambiar_el_documento_del_jugador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var cliente = await EntrarAsync(luis.NumeroDocumento);

        var renovacion = await cliente.PostAsync("/api/sesion/renovacion");
        Assert.Equal(HttpStatusCode.OK, renovacion.StatusCode);
        cliente.UsarToken((await renovacion.JsonAsync()).GetProperty("token").GetString()!);

        Assert.Empty((await ClubesAsync(cliente)).Single().Lista("jugadores"));
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub)).StatusCode);
        cliente.ElegirJugador((Guid?)null);

        // Se guardan identificadores y no el número: cambiarlo no rompe la sesión.
        var cambio = await cliente.PutAsync(
            $"{EscenarioFicha.Ficha(e.Club.Id, luis.Id)}/documento-identidad",
            new { tipoDocumento = "TARJETA_IDENTIDAD", numeroDocumento = Sembrador.Unico("ti") });
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);

        var despues = await cliente.GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.OK, despues.StatusCode);
        Assert.Equal(luis.Id, (await despues.JsonAsync()).GetProperty("miUsuarioRolId").GetGuid());
        Assert.Equal(luis.Id, (await ClubesAsync(cliente)).Single().GetProperty("usuarioRolId").GetGuid());
    }

    // Caso límite: el hermano que sigue en espera.
    [Fact]
    public async Task Entrar_con_el_documento_de_un_hermano_en_espera_solo_muestra_que_esta_pendiente()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var cliente = await EntrarAsync(enEspera.NumeroDocumento);

        var club = (await ClubesAsync(cliente)).Single();
        var delClub = await cliente.GetAsync(e.InicioDelClub);

        Assert.Equal("EN_ESPERA", club.GetProperty("estadoIngreso").GetString());
        Assert.Equal(enEspera.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.Empty(club.Lista("jugadores"));
        Assert.Equal(HttpStatusCode.Forbidden, delClub.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(delClub));
    }

    [Fact]
    public async Task Entrar_con_el_correo_de_la_misma_cuenta_no_tiene_limitacion()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var cliente = await EntrarAsync(e.Familia.Correo);

        Assert.Equal([e.Ana.Id, luis.Id], (await ClubesAsync(cliente)).Single().Lista("jugadores").Ids("usuarioRolId"));
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.ElegirJugador(luis.Id).GetAsync(e.InicioDelClub)).StatusCode);
    }

    // Research §3: la limitación no cambia nada donde la cuenta tiene un solo integrante.
    [Fact]
    public async Task Sigue_viendo_el_club_donde_tiene_un_solo_integrante_y_no_el_club_donde_ninguno_tiene_ese_documento()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        UsuarioRol enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Familia, Rol.ENTRENADOR);
        var inicioDelOtro = $"/api/clubes/{e.OtroClub.Id}";

        var conElDeLuis = await EntrarAsync(luis.NumeroDocumento);
        Assert.Equal(
            new[] { e.Club.Id, e.OtroClub.Id }.Order(),
            (await ClubesAsync(conElDeLuis)).Ids("clubId").Order());
        Assert.Equal(HttpStatusCode.OK, (await conElDeLuis.GetAsync(inicioDelOtro)).StatusCode);

        // Con el documento que usa en el otro club, el club de los hermanos no aparece ni responde.
        var conElDelOtro = await EntrarAsync(enElOtro.NumeroDocumento);
        var clubes = await ClubesAsync(conElDelOtro);
        var delClubDeLosHermanos = await conElDelOtro.GetAsync(e.InicioDelClub);

        Assert.Equal([e.OtroClub.Id], clubes.Ids("clubId"));
        Assert.Equal(HttpStatusCode.OK, (await conElDelOtro.GetAsync(inicioDelOtro)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delClubDeLosHermanos.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(delClubDeLosHermanos));
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await conElDelOtro.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub)).StatusCode);
    }
}
