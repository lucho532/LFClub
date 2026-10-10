using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;

namespace LaPecosa.Pruebas.Integracion.Ficha;

/// <summary>
/// Cambiar el documento de identidad actualiza al mismo jugador, que desde entonces entra con el
/// número nuevo (historia 4, escenarios 1 a 3; RF-022 a RF-024; CE-007 y CE-008; supuesto 1).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CambioDeDocumentoPruebas
{
    private readonly FabricaApi _fabrica;

    public CambioDeDocumentoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Cuerpo(string numero, string tipo = "TARJETA_IDENTIDAD") =>
        new { tipoDocumento = tipo, numeroDocumento = numero };

    // Escenario 4.1 y CE-007.
    [Fact]
    public async Task La_familia_cambia_el_documento_y_el_jugador_conserva_todo_lo_demas()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var equipo = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "A");
        await e.Base.Sembrar.PonerEnEquipoAsync(e.Ana, equipo);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(64), "application/pdf");
        var antes = await e.JugadorGuardadoAsync(e.Ana.Id);
        var nuevo = Sembrador.Unico("ti");

        // Se guarda normalizado: sin espacios ni puntos y en minúsculas.
        var respuesta = await e.CuentaDeAna.PutAsync(
            e.DocumentoIdentidad(e.Ana.Id), Cuerpo($" {nuevo[..4].ToUpperInvariant()}.{nuevo[4..]} ", "CEDULA_EXTRANJERIA"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.SinCache());
        var ficha = await respuesta.JsonAsync();
        Assert.Equal("CEDULA_EXTRANJERIA", ficha.GetProperty("tipoDocumento").GetString());
        Assert.Equal(nuevo, ficha.GetProperty("numeroDocumento").GetString());
        Assert.True(ficha.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());

        var despues = await e.JugadorGuardadoAsync(e.Ana.Id);
        Assert.Equal(TipoDocumento.CEDULA_EXTRANJERIA, despues.TipoDocumento);
        Assert.Equal(nuevo, despues.NumeroDocumento);
        Assert.Equal(antes.UsuarioId, despues.UsuarioId);
        Assert.Equal(e.CategoriaDeAna.Id, despues.CategoriaId);
        Assert.Equal([equipo.Id], despues.Equipos.Select(fila => fila.EquipoId));
        Assert.Equal((antes.Nombres, antes.Apellidos, antes.FechaNacimiento), (despues.Nombres, despues.Apellidos, despues.FechaNacimiento));
        Assert.Equal((antes.EstadoIngreso, antes.Activo, antes.Rol), (despues.EstadoIngreso, despues.Activo, despues.Rol));
        Assert.Equal(SembradorFichas.Alergias, (await e.FichaGuardadaAsync(e.Ana.Id))!.Alergias);
        Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id));
    }

    // Escenario 4.2 y RF-024: entra con el nuevo y ya no con el anterior; la sesión abierta sigue sirviendo.
    [Fact]
    public async Task Inicia_sesion_con_el_numero_nuevo_y_con_el_anterior_ya_no()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var anterior = e.Beto.NumeroDocumento;
        var nuevo = Sembrador.Unico("ti");

        var cambio = await e.CuentaDeBeto.PutAsync(e.DocumentoIdentidad(e.Beto.Id), Cuerpo(nuevo));

        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(nuevo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(anterior)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.CuentaDeBeto.GetAsync(e.Ficha(e.Beto.Id))).StatusCode);
    }

    // Escenario 4.3 y CE-008: de otro integrante activo, de uno retirado y de quien no es jugador.
    [Fact]
    public async Task El_numero_de_otro_integrante_del_club_responde_409_y_nada_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var ocupados = new[] { e.Beto.NumeroDocumento, e.Dani.NumeroDocumento, e.Base.IntegranteEntrenador.NumeroDocumento };

        foreach (var ocupado in ocupados)
        {
            var respuesta = await e.CuentaDeAna.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(ocupado));

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal("documento_repetido_en_club", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(e.Ana.NumeroDocumento, (await e.JugadorGuardadoAsync(e.Ana.Id)).NumeroDocumento);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    // Supuesto 1: la misma regla del registro.
    [Fact]
    public async Task El_numero_de_otra_cuenta_en_otro_club_responde_409()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, ajeno) = await e.Base.Sembrar.CrearJugadorAsync(e.Base.OtroClub, 2014);

        var respuesta = await e.CuentaDeAna.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(ajeno.NumeroDocumento));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(e.Ana.NumeroDocumento, (await e.JugadorGuardadoAsync(e.Ana.Id)).NumeroDocumento);
    }

    // Research §16 de la 006: con el número en dos cuentas, la respuesta no depende de cuál devuelva la consulta.
    [Fact]
    public async Task Un_numero_que_esta_en_dos_cuentas_responde_409_a_una_tercera_y_lo_conserva_quien_ya_lo_tiene()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, ajeno) = await e.Base.Sembrar.CrearJugadorAsync(e.Base.OtroClub, 2014);
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(
            e.Ana, EstadoIngreso.APROBADO, numeroDocumento: ajeno.NumeroDocumento);
        var tercerClub = await _fabrica.Sembrador.CrearClubAsync();
        var (cuentaTercera, tercero) = await e.Base.Sembrar.CrearJugadorAsync(tercerClub, 2014);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaTercera);

        var respuesta = await cliente.PutAsync(
            $"{EscenarioFicha.Ficha(tercerClub.Id, tercero.Id)}/documento-identidad", Cuerpo(ajeno.NumeroDocumento));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("documento_en_otra_cuenta", await ClienteDePrueba.CodigoAsync(respuesta));

        // El hermano conserva su número y puede cambiar el tipo.
        var delHermano = await e.Presidente.PutAsync(
            e.DocumentoIdentidad(hermano.Id), Cuerpo(ajeno.NumeroDocumento, "REGISTRO_CIVIL"));
        Assert.Equal(HttpStatusCode.OK, delHermano.StatusCode);
    }

    // El documento se cambia club por club: el otro club conserva el anterior, que sigue sirviendo para entrar.
    [Fact]
    public async Task Una_persona_en_dos_clubes_lo_cambia_en_uno_y_el_otro_conserva_el_anterior()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var cuenta = (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!;
        var enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(
            e.Base.OtroClub, cuenta, Rol.JUGADOR, documento: e.Ana.NumeroDocumento);
        var nuevo = Sembrador.Unico("ti");

        var respuesta = await e.CuentaDeAna.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(nuevo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(nuevo, (await e.JugadorGuardadoAsync(e.Ana.Id)).NumeroDocumento);
        Assert.Equal(e.Ana.NumeroDocumento, (await e.JugadorGuardadoAsync(enElOtro.Id)).NumeroDocumento);
        Assert.Equal(HttpStatusCode.OK, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(e.Ana.NumeroDocumento)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _fabrica.CrearClienteDePrueba().IniciarSesionAsync(nuevo)).StatusCode);

        // Ponerle en el otro club el número que ya usa su misma cuenta sí se admite (§10).
        var enElOtroClub = await e.CuentaDeAna.PutAsync(
            $"{EscenarioFicha.Ficha(e.Base.OtroClub.Id, enElOtro.Id)}/documento-identidad", Cuerpo(nuevo));
        Assert.Equal(HttpStatusCode.OK, enElOtroClub.StatusCode);
    }

    [Fact]
    public async Task Enviar_el_mismo_documento_no_sella_ningun_cambio()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(e.Ana.NumeroDocumento));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False((await respuesta.JsonAsync()).Tiene("ultimoCambio"));
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    // De registro civil a tarjeta de identidad el número suele ser el mismo: no es un número repetido.
    [Fact]
    public async Task Cambiar_solo_el_tipo_conservando_el_numero_se_guarda()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.PutAsync(
            e.DocumentoIdentidad(e.Ana.Id), Cuerpo(e.Ana.NumeroDocumento, "REGISTRO_CIVIL"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(TipoDocumento.REGISTRO_CIVIL, (await e.JugadorGuardadoAsync(e.Ana.Id)).TipoDocumento);
        Assert.NotNull(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    [Theory]
    [InlineData("OTRO_TIPO", "123456", "tipoDocumento")]
    [InlineData("TARJETA_IDENTIDAD", "", "numeroDocumento")]
    [InlineData("TARJETA_IDENTIDAD", "123456789012345678901", "numeroDocumento")]
    public async Task Un_tipo_desconocido_o_un_numero_no_valido_responde_400(string tipo, string numero, string campo)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(numero, tipo));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Contains(campo, await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(e.Ana.NumeroDocumento, (await e.JugadorGuardadoAsync(e.Ana.Id)).NumeroDocumento);
    }

    [Fact]
    public async Task El_presidente_tambien_lo_cambia_tambien_a_un_retirado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        foreach (var jugador in new[] { e.Ana, e.Dani })
        {
            var nuevo = Sembrador.Unico("ti");

            var respuesta = await e.Presidente.PutAsync(e.DocumentoIdentidad(jugador.Id), Cuerpo(nuevo));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(nuevo, (await e.JugadorGuardadoAsync(jugador.Id)).NumeroDocumento);
            Assert.False((await respuesta.JsonAsync()).GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());
        }
    }

    [Fact]
    public async Task Otra_cuenta_de_jugador_recibe_404_y_nada_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeBeto.PutAsync(e.DocumentoIdentidad(e.Ana.Id), Cuerpo(Sembrador.Unico("ti")));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(e.Ana.NumeroDocumento, (await e.JugadorGuardadoAsync(e.Ana.Id)).NumeroDocumento);
    }
}
