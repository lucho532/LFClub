using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;

namespace LaPecosa.Pruebas.Integracion.Ficha;

/// <summary>
/// El PRESIDENTE corrige los nombres, los apellidos y la fecha de nacimiento; corregir la
/// fecha no mueve a quien ya tiene categoría y ubica a quien no tiene (historia 4, escenarios 4 a
/// 7; historia 1, escenario 6; RF-017, RF-018, RF-021, RF-025, RF-026; supuesto 2). Lo que
/// corrige la familia (spec 010, que cambia RF-016 y RF-017 de la 005) está en
/// <see cref="CorreccionPorLaFamiliaPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CorreccionDeIdentidadPruebas
{
    private readonly FabricaApi _fabrica;

    public CorreccionDeIdentidadPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Cuerpo(UsuarioRol jugador, string? nombres = null, string? apellidos = null, string? fecha = null) => new
    {
        nombres = nombres ?? jugador.Nombres,
        apellidos = apellidos ?? jugador.Apellidos,
        fechaNacimiento = fecha ?? jugador.FechaNacimiento.ToString("yyyy-MM-dd"),
    };

    // Escenario 4.4: los ven la lista de la categoría y la sesión del jugador.
    [Fact]
    public async Task El_presidente_corrige_nombres_y_apellidos_y_se_ven_en_la_lista_y_en_la_sesion()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var apellidos = $"Restrepo {Sembrador.Unico()}";

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Ana.Id), Cuerpo(e.Ana, "  Ana   María ", apellidos));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.SinCache());
        var ficha = await respuesta.JsonAsync();
        Assert.Equal("Ana María", ficha.GetProperty("nombres").GetString());
        Assert.Equal(apellidos, ficha.GetProperty("apellidos").GetString());
        Assert.False(ficha.GetProperty("ultimoCambio").GetProperty("porLaCuentaDelJugador").GetBoolean());

        var enLaLista = (await e.Base.DetalleAsync(e.CategoriaDeAna.Id)).Lista("jugadores")
            .Single(jugador => jugador.GetProperty("usuarioRolId").GetGuid() == e.Ana.Id);
        Assert.Equal("Ana María", enLaLista.GetProperty("nombres").GetString());
        Assert.Equal(apellidos, enLaLista.GetProperty("apellidos").GetString());
        var enLaSesion = (await (await e.CuentaDeAna.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes")
            .Single(club => club.GetProperty("clubId").GetGuid() == e.Club.Id);
        Assert.Equal("Ana María", enLaSesion.GetProperty("nombres").GetString());
        Assert.Equal(apellidos, enLaSesion.GetProperty("apellidos").GetString());
    }

    // Escenario 4.5 y RF-025.
    [Fact]
    public async Task Corregir_la_fecha_de_un_jugador_con_categoria_lo_deja_en_la_que_tenia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Ana.Id), Cuerpo(e.Ana, fecha: "2015-03-02"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ficha = await respuesta.JsonAsync();
        Assert.Equal("2015-03-02", ficha.GetProperty("fechaNacimiento").GetString());
        Assert.Equal(e.CategoriaDeAna.Id, ficha.GetProperty("categoriaId").GetGuid());
        Assert.Equal(2014, ficha.GetProperty("categoriaAnio").GetInt32());
        Assert.Equal(e.CategoriaDeAna.Id, (await e.JugadorGuardadoAsync(e.Ana.Id)).CategoriaId);
    }

    // Escenario 4.6 y RF-026.
    [Fact]
    public async Task Corregir_la_fecha_de_un_jugador_sin_categoria_lo_ubica_en_la_activa_del_anio_nuevo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Caro.Id), Cuerpo(e.Caro, fecha: "2015-11-30"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ficha = await respuesta.JsonAsync();
        Assert.Equal(e.CategoriaDeBeto.Id, ficha.GetProperty("categoriaId").GetGuid());
        Assert.Equal(2015, ficha.GetProperty("categoriaAnio").GetInt32());
        Assert.Equal(e.CategoriaDeBeto.Id, (await e.JugadorGuardadoAsync(e.Caro.Id)).CategoriaId);
        Assert.True((await e.Base.CategoriaGuardadaAsync(e.CategoriaDeBeto.Id))!.Usada);
    }

    [Fact]
    public async Task Hacia_un_anio_sin_categoria_o_con_la_categoria_inactiva_sigue_sin_categoria()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.Base.Sembrar.CrearCategoriaAsync(e.Club, 2012, activa: false);

        foreach (var fecha in new[] { "2011-01-15", "2012-01-15" })
        {
            var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Caro.Id), Cuerpo(e.Caro, fecha: fecha));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal(fecha, (await respuesta.JsonAsync()).GetProperty("fechaNacimiento").GetString());
            Assert.Null((await e.JugadorGuardadoAsync(e.Caro.Id)).CategoriaId);
        }
    }

    [Fact]
    public async Task Corregir_la_fecha_de_un_retirado_no_lo_ubica()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Dani.Id), Cuerpo(e.Dani, fecha: "2015-06-01"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = await e.JugadorGuardadoAsync(e.Dani.Id);
        Assert.Equal(new DateOnly(2015, 6, 1), guardado.FechaNacimiento);
        Assert.Null(guardado.CategoriaId);
        Assert.False(guardado.Activo);
    }

    // Escenario 4.7 y RF-021.
    [Fact]
    public async Task Una_fecha_futura_responde_400_en_ese_campo_y_nada_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var manana = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Ana.Id), Cuerpo(e.Ana, "Otro", fecha: manana));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True((await respuesta.JsonAsync()).GetProperty("errores").Tiene("fechaNacimiento"));
        var guardado = await e.JugadorGuardadoAsync(e.Ana.Id);
        Assert.Equal(e.Ana.Nombres, guardado.Nombres);
        Assert.Equal(e.Ana.FechaNacimiento, guardado.FechaNacimiento);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    [Theory]
    [InlineData("", "nombres")]
    [InlineData("   ", "nombres")]
    [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901", "nombres")]
    public async Task Unos_nombres_vacios_o_de_81_caracteres_responden_400(string nombres, string campo)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Ana.Id), new
        {
            nombres, apellidos = e.Ana.Apellidos, fechaNacimiento = "2014-07-15",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await respuesta.JsonAsync()).GetProperty("errores").Tiene(campo));
        Assert.Equal(e.Ana.Nombres, (await e.JugadorGuardadoAsync(e.Ana.Id)).Nombres);
    }

    // Con el club bloqueado, gane quien gane el jugador queda dentro (patrón de la 003).
    [Fact]
    public async Task Corregir_la_fecha_mientras_se_crea_la_categoria_de_ese_anio_lo_deja_siempre_dentro()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var otroPresidente = await _fabrica.CrearClienteDePrueba()
            .ConSesionDeAsync((await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE)).Usuario);

        for (var vuelta = 0; vuelta < 5; vuelta++)
        {
            var anio = 2000 + vuelta;
            var (_, jugador) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 1990);

            var respuestas = await Task.WhenAll(
                e.Presidente.PutAsync(e.Identidad(jugador.Id), Cuerpo(jugador, fecha: $"{anio}-05-05")),
                otroPresidente.PostAsync(e.Base.Categorias, new { anio }));

            Assert.Equal(HttpStatusCode.OK, respuestas[0].StatusCode);
            Assert.Equal(HttpStatusCode.Created, respuestas[1].StatusCode);
            var categoriaId = (await respuestas[1].JsonAsync()).GetProperty("categoria").GetProperty("categoriaId").GetGuid();
            Assert.Equal(categoriaId, (await e.JugadorGuardadoAsync(jugador.Id)).CategoriaId);
        }
    }

    // §13: lo ya copiado en un retiro no cambia aunque después se corrija el nombre.
    [Fact]
    public async Task El_nombre_copiado_en_un_retiro_anterior_no_cambia()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var copiado = (await e.JugadorGuardadoAsync(e.Dani.Id)).RetiradoPorNombre;

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Dani.Id), Cuerpo(e.Dani, "Daniel", "Corregido"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = await e.JugadorGuardadoAsync(e.Dani.Id);
        Assert.Equal("Daniel", guardado.Nombres);
        Assert.Equal(copiado, guardado.RetiradoPorNombre);
        Assert.NotNull(guardado.RetiradoEn);
    }

    // Supuesto 2: la corrección se guarda aunque el jugador pase a ser menor y no tenga responsable.
    [Fact]
    public async Task Corregir_la_fecha_no_exige_responsable_aunque_el_jugador_pase_a_ser_menor()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuenta, adulto) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 1995);
        Assert.Null(cuenta.NombreResponsable);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(adulto.Id), Cuerpo(adulto, fecha: "2019-02-02"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await respuesta.JsonAsync()).GetProperty("esMenorDeEdad").GetBoolean());

        // Se exige la próxima vez que alguien guarde el contacto.
        var contacto = await e.Presidente.PutAsync(e.Ficha(adulto.Id), EscenarioFicha.Cuerpo(nombreResponsable: null));
        Assert.Equal(HttpStatusCode.BadRequest, contacto.StatusCode);
    }

    [Fact]
    public async Task Corregir_sin_cambiar_nada_no_sella_ningun_cambio()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.Presidente.PutAsync(e.Identidad(e.Ana.Id), Cuerpo(e.Ana));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }
}
