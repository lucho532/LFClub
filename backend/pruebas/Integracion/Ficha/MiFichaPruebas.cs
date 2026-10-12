using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// La familia consulta y mantiene la ficha de su jugador (historia 1, escenarios 1 a 6 y 10;
/// RF-001, RF-002, RF-005, RF-016, RF-017, RF-020; CE-001).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MiFichaPruebas
{
    private readonly FabricaApi _fabrica;

    public MiFichaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_su_identidad_su_categoria_sus_equipos_y_su_contacto()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var equipoB = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "B");
        var equipoA = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "A");
        var viejo = await e.Base.Sembrar.CrearEquipoAsync(e.CategoriaDeAna, "Viejo", activo: false);
        foreach (var equipo in new[] { equipoB, equipoA, viejo })
        {
            await e.Base.Sembrar.PonerEnEquipoAsync(e.Ana, equipo);
        }

        var respuesta = await e.CuentaDeAna.GetAsync(e.Ficha(e.Ana.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.SinCache());
        var ficha = await respuesta.JsonAsync();
        var cuenta = (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!;
        Assert.Equal(e.Ana.Id, ficha.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(e.Ana.Nombres, ficha.GetProperty("nombres").GetString());
        Assert.Equal(e.Ana.Apellidos, ficha.GetProperty("apellidos").GetString());
        Assert.Equal("TARJETA_IDENTIDAD", ficha.GetProperty("tipoDocumento").GetString());
        Assert.Equal(e.Ana.NumeroDocumento, ficha.GetProperty("numeroDocumento").GetString());
        Assert.Equal("2014-07-15", ficha.GetProperty("fechaNacimiento").GetString());
        Assert.True(ficha.GetProperty("esMenorDeEdad").GetBoolean());
        Assert.False(ficha.GetProperty("retirado").GetBoolean());
        Assert.Equal(e.CategoriaDeAna.Id, ficha.GetProperty("categoriaId").GetGuid());
        Assert.Equal(2014, ficha.GetProperty("categoriaAnio").GetInt32());
        Assert.Equal(["A", "B"], ficha.Lista("equipos").Select(equipo => equipo.GetString()));
        var contacto = ficha.GetProperty("contacto");
        Assert.Equal(cuenta.Correo, contacto.GetProperty("correo").GetString());
        Assert.Equal(cuenta.Celular, contacto.GetProperty("celular").GetString());
        Assert.Equal(JsonValueKind.Null, contacto.GetProperty("nombreResponsable").ValueKind);
    }

    // Escenario 1.10: nadie la ha cambiado desde el registro.
    [Fact]
    public async Task Sin_ficha_guardada_los_grupos_llegan_vacios_los_documentos_pendientes_y_sin_ultimo_cambio()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var ficha = await e.FichaDeAsync(e.CuentaDeCaro, e.Caro.Id);

        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("categoriaId").ValueKind);
        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("categoriaAnio").ValueKind);
        Assert.Empty(ficha.Lista("equipos"));
        foreach (var grupo in new[] { "contactoEmergencia", "seguridadSocial", "datosClinicos" })
        {
            Assert.All(ficha.GetProperty(grupo).EnumerateObject(), dato => Assert.Equal(JsonValueKind.Null, dato.Value.ValueKind));
        }

        Assert.Equal(5, ficha.GetProperty("datosClinicos").EnumerateObject().Count());
        Assert.Equal(
            ["COPIA_DOCUMENTO_IDENTIDAD", "CERTIFICADO_SALUD"],
            ficha.Lista("documentos").Select(documento => documento.GetProperty("documento").GetString()));
        Assert.All(ficha.Lista("documentos"), documento =>
        {
            Assert.False(documento.GetProperty("entregado").GetBoolean());
            Assert.Equal(JsonValueKind.Null, documento.GetProperty("subidoEn").ValueKind);
        });
        Assert.False(ficha.Tiene("ultimoCambio"));
        Assert.True(ficha.GetProperty("permisos").GetProperty("puedeCambiar").GetBoolean());
        // Spec 010 (RF-013): la familia también corrige nombres, apellidos y fecha de nacimiento.
        Assert.True(ficha.GetProperty("permisos").GetProperty("puedeCorregirIdentidad").GetBoolean());
    }

    [Fact]
    public async Task Con_una_ficha_sembrada_devuelve_exactamente_lo_sembrado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);

        var ficha = await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id);

        var emergencia = ficha.GetProperty("contactoEmergencia");
        Assert.Equal(SembradorFichas.EmergenciaNombre, emergencia.GetProperty("nombre").GetString());
        Assert.Equal(SembradorFichas.EmergenciaParentesco, emergencia.GetProperty("parentesco").GetString());
        Assert.Equal(SembradorFichas.EmergenciaCelular, emergencia.GetProperty("celular").GetString());
        Assert.Equal(SembradorFichas.EntidadSalud, ficha.GetProperty("seguridadSocial").GetProperty("entidadSalud").GetString());
        Assert.Equal(SembradorFichas.LugarAtencion, ficha.GetProperty("seguridadSocial").GetProperty("lugarAtencion").GetString());
        var clinicos = ficha.GetProperty("datosClinicos");
        Assert.Equal("O_NEGATIVO", clinicos.GetProperty("grupoSanguineo").GetString());
        Assert.Equal(SembradorFichas.Alergias, clinicos.GetProperty("alergias").GetString());
        Assert.Equal(SembradorFichas.Enfermedades, clinicos.GetProperty("enfermedades").GetString());
        Assert.Equal(SembradorFichas.Medicamentos, clinicos.GetProperty("medicamentos").GetString());
        Assert.Equal(SembradorFichas.Observaciones, clinicos.GetProperty("observaciones").GetString());
    }

    // Escenarios 1.2 y 1.3 y CE-001: lo guardado es lo que se ve al volver a entrar.
    [Fact]
    public async Task Guardar_y_volver_a_leer_con_otra_sesion_devuelve_exactamente_lo_guardado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var cuerpo = EscenarioFicha.Cuerpo();
        cuerpo["alergias"] = "  Polen\ny ácaros  ";
        cuerpo["entidadSalud"] = "  Nueva   EPS ";

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.SinCache());
        var cuentaDeAna = (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!;
        var otraSesion = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaDeAna);
        foreach (var ficha in new[] { await respuesta.JsonAsync(), await e.FichaDeAsync(otraSesion, e.Ana.Id) })
        {
            Assert.Equal(EscenarioFicha.CelularNuevo, ficha.GetProperty("contacto").GetProperty("celular").GetString());
            Assert.Equal(EscenarioFicha.ResponsableNuevo, ficha.GetProperty("contacto").GetProperty("nombreResponsable").GetString());
            Assert.Equal("Luis Gómez", ficha.GetProperty("contactoEmergencia").GetProperty("nombre").GetString());
            Assert.Equal("Padre", ficha.GetProperty("contactoEmergencia").GetProperty("parentesco").GetString());
            Assert.Equal("3151112233", ficha.GetProperty("contactoEmergencia").GetProperty("celular").GetString());
            Assert.Equal("Nueva EPS", ficha.GetProperty("seguridadSocial").GetProperty("entidadSalud").GetString());
            Assert.Equal("Hospital de Caldas", ficha.GetProperty("seguridadSocial").GetProperty("lugarAtencion").GetString());
            Assert.Equal("A_POSITIVO", ficha.GetProperty("datosClinicos").GetProperty("grupoSanguineo").GetString());
            Assert.Equal("Polen\ny ácaros", ficha.GetProperty("datosClinicos").GetProperty("alergias").GetString());
            Assert.Equal("Ninguna conocida", ficha.GetProperty("datosClinicos").GetProperty("enfermedades").GetString());
            Assert.Equal("Loratadina", ficha.GetProperty("datosClinicos").GetProperty("medicamentos").GetString());
            Assert.Equal("Usa gafas", ficha.GetProperty("datosClinicos").GetProperty("observaciones").GetString());
        }
    }

    // Escenario 1.4 y RF-002: reemplaza el formulario entero; un campo vacío o ausente queda vacío.
    [Fact]
    public async Task Todo_vacio_es_valido_y_deja_los_datos_en_nulo()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        var cuerpo = new Dictionary<string, object?>
        {
            ["celular"] = "3001112233", ["nombreResponsable"] = "Marta Gómez", ["alergias"] = "   ", ["entidadSalud"] = "",
        };

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardada = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        Assert.Null(guardada.EmergenciaNombre);
        Assert.Null(guardada.EmergenciaParentesco);
        Assert.Null(guardada.EmergenciaCelular);
        Assert.Null(guardada.EntidadSalud);
        Assert.Null(guardada.LugarAtencion);
        Assert.Null(guardada.GrupoSanguineo);
        Assert.Null(guardada.Alergias);
        Assert.Null(guardada.Enfermedades);
        Assert.Null(guardada.Medicamentos);
        Assert.Null(guardada.Observaciones);
    }

    // Escenario 1.5 y RF-020.
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Un_menor_sin_responsable_recibe_400_en_ese_campo_y_nada_cambia(string? responsable)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo(nombreResponsable: responsable));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True((await respuesta.JsonAsync()).GetProperty("errores").Tiene("nombreResponsable"));
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
        Assert.Equal("3001234567", (await e.JugadorGuardadoAsync(e.Ana.Id)).Usuario!.Celular);
    }

    [Fact]
    public async Task Un_jugador_adulto_no_es_menor_y_guarda_sin_responsable()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuenta, adulto) = await e.Base.Sembrar.CrearJugadorAsync(e.Club, 1995);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);
        Assert.False((await e.FichaDeAsync(cliente, adulto.Id)).GetProperty("esMenorDeEdad").GetBoolean());

        var respuesta = await cliente.PutAsync(e.Ficha(adulto.Id), EscenarioFicha.Cuerpo(nombreResponsable: null));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null((await e.JugadorGuardadoAsync(adulto.Id)).Usuario!.NombreResponsable);
        Assert.Equal(EscenarioFicha.CelularNuevo, (await e.JugadorGuardadoAsync(adulto.Id)).Usuario!.Celular);
    }

    [Theory]
    [InlineData("celular", "")]
    [InlineData("emergenciaParentesco", "12345678901234567890123456789012345678901")]
    [InlineData("grupoSanguineo", "Z_POSITIVO")]
    public async Task Un_dato_no_valido_recibe_400_y_no_se_guarda_nada(string campo, string valor)
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var cuerpo = EscenarioFicha.Cuerpo();
        cuerpo[campo] = valor;

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Null(await e.FichaGuardadaAsync(e.Ana.Id));
    }

    // Escenario 1.6 y RF-017: lo que llega de más en el cuerpo se ignora.
    [Fact]
    public async Task La_identidad_el_documento_y_el_correo_enviados_en_el_cuerpo_se_ignoran()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var antes = await e.JugadorGuardadoAsync(e.Ana.Id);
        var cuerpo = EscenarioFicha.Cuerpo();
        cuerpo["nombres"] = "Otro";
        cuerpo["apellidos"] = "Distinto";
        cuerpo["fechaNacimiento"] = "2001-01-01";
        cuerpo["tipoDocumento"] = "CEDULA_CIUDADANIA";
        cuerpo["numeroDocumento"] = "999999";
        cuerpo["correo"] = "otro@lfclub.test";

        var respuesta = await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var despues = await e.JugadorGuardadoAsync(e.Ana.Id);
        Assert.Equal(antes.Nombres, despues.Nombres);
        Assert.Equal(antes.Apellidos, despues.Apellidos);
        Assert.Equal(antes.FechaNacimiento, despues.FechaNacimiento);
        Assert.Equal(TipoDocumento.TARJETA_IDENTIDAD, despues.TipoDocumento);
        Assert.Equal(antes.NumeroDocumento, despues.NumeroDocumento);
        Assert.Equal(antes.Usuario!.Correo, despues.Usuario!.Correo);
        Assert.Equal(EscenarioFicha.CelularNuevo, despues.Usuario.Celular);
    }
}
