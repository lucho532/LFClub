using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using LaPecosa.Pruebas.Integracion.Ficha;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Aprobar a un hermano lo ubica como a quien entra con una invitación de JUGADOR y le da su
/// propia ficha, vacía; solo el PRESIDENTE lo aprueba o lo rechaza, y un documento rechazado se
/// puede volver a agregar (historia 3, escenarios 2, 3 y 6 a 8; RF-016, RF-017, RF-019 y RF-020;
/// CE-004). La aprobación es la de la 004, sin cambios.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AprobarHermanoPruebas
{
    private readonly FabricaApi _fabrica;

    public AprobarHermanoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Aprobacion(EscenarioHermanos e, Guid id) => $"/api/clubes/{e.Club.Id}/ingresos/{id}/aprobacion";

    private static string Rechazo(EscenarioHermanos e, Guid id) => $"/api/clubes/{e.Club.Id}/ingresos/{id}/rechazo";

    private static string EnEspera(EscenarioHermanos e) => $"/api/clubes/{e.Club.Id}/ingresos/en-espera";

    private static async Task<UsuarioRol> GuardadoAsync(EscenarioHermanos e, Guid id) =>
        (await e.JugadoresDeLaFamiliaAsync()).Single(jugador => jugador.Id == id);

    // Escenario 3.2, RF-016 y CE-004.
    [Fact]
    public async Task Queda_aprobado_como_jugador_en_la_categoria_activa_de_su_anio_o_sin_categoria_si_no_existe()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var conCategoria = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Luis");
        var sinCategoria = await e.Sembrar.CrearHermanoAsync(e.Ana, fechaNacimiento: new DateOnly(2016, 2, 2), nombres: "Mara");

        foreach (var hermano in new[] { conCategoria, sinCategoria })
        {
            var respuesta = await e.Presidente.PostAsync(Aprobacion(e, hermano.Id));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("JUGADOR", (await respuesta.JsonAsync()).GetProperty("rolDeIngreso").GetString());
        }

        var luis = await GuardadoAsync(e, conCategoria.Id);
        var mara = await GuardadoAsync(e, sinCategoria.Id);
        Assert.Equal((EstadoIngreso.APROBADO, Rol.JUGADOR, true), (luis.EstadoIngreso, luis.Rol, luis.Activo));
        Assert.Equal(e.CategoriaDeAna.Id, luis.CategoriaId);
        Assert.Empty(luis.Equipos);
        Assert.Equal(e.Base.IntegrantePresidente.UsuarioId, luis.AprobadoPorUsuarioId);
        Assert.Equal((EstadoIngreso.APROBADO, Rol.JUGADOR), (mara.EstadoIngreso, mara.Rol));
        Assert.Null(mara.CategoriaId);

        var enEspera = (await JsonDeCategorias.ListaAsync(e.Presidente, EnEspera(e))).Ids("usuarioRolId");
        Assert.DoesNotContain(luis.Id, enEspera);
        Assert.DoesNotContain(mara.Id, enEspera);

        // Ya aparecen en las listas del club: uno en su categoría y el otro en "Sin categoría".
        var deLaCategoria = (await e.Base.DetalleAsync(e.CategoriaDeAna.Id)).Lista("jugadores").Ids("usuarioRolId");
        Assert.Contains(luis.Id, deLaCategoria);
        Assert.Contains(mara.Id, (await JsonDeCategorias.ListaAsync(e.Presidente, e.Base.SinCategoria)).Ids("usuarioRolId"));
    }

    // Escenarios 3.3 y 3.8 y RF-017.
    [Fact]
    public async Task Recien_aprobado_la_familia_entra_con_el_y_su_ficha_nace_vacia_sin_tocar_la_del_primer_hijo()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var conElHermano = await e.ClienteDeLaFamiliaAsync(hermano.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await conElHermano.GetAsync(e.InicioDelClub)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e, hermano.Id))).StatusCode);

        // La familia vuelve a cargar con el mismo jugador elegido y ya entra.
        var delClub = await conElHermano.GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.OK, delClub.StatusCode);
        Assert.Equal(hermano.Id, (await delClub.JsonAsync()).GetProperty("miUsuarioRolId").GetGuid());

        var ficha = await (await conElHermano.GetAsync(EscenarioFicha.Ficha(e.Club.Id, hermano.Id))).JsonAsync();
        Assert.Equal("Luis", ficha.GetProperty("nombres").GetString());
        Assert.Equal(hermano.NumeroDocumento, ficha.GetProperty("numeroDocumento").GetString());
        Assert.Equal(EscenarioHermanos.AnioDeAna, ficha.GetProperty("categoriaAnio").GetInt32());
        Assert.Equal(e.Familia.Correo, ficha.GetProperty("contacto").GetProperty("correo").GetString());
        Assert.Equal("3001234567", ficha.GetProperty("contacto").GetProperty("celular").GetString());
        foreach (var (grupo, dato) in new[]
        {
            ("contactoEmergencia", "nombre"), ("contactoEmergencia", "celular"), ("seguridadSocial", "entidadSalud"),
            ("seguridadSocial", "lugarAtencion"), ("datosClinicos", "grupoSanguineo"), ("datosClinicos", "alergias"),
        })
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Null, ficha.GetProperty(grupo).GetProperty(dato).ValueKind);
        }

        var documentos = ficha.Lista("documentos");
        Assert.Equal(2, documentos.Count);
        Assert.All(documentos, documento => Assert.False(documento.GetProperty("entregado").GetBoolean()));
        Assert.False(ficha.Tiene("ultimoCambio"));

        // La de Ana no cambió.
        var deAna = await (await conElHermano.ElegirJugador(e.Ana.Id).GetAsync(EscenarioFicha.Ficha(e.Club.Id, e.Ana.Id))).JsonAsync();
        Assert.Equal(SembradorFichas.Alergias, deAna.GetProperty("datosClinicos").GetProperty("alergias").GetString());
        Assert.True(deAna.DocumentoDe(DocumentoPedido.CERTIFICADO_SALUD).GetProperty("entregado").GetBoolean());
    }

    // Escenario 3.7 y RF-020.
    [Fact]
    public async Task Solo_el_presidente_ve_aprueba_y_rechaza_a_un_hermano_en_espera()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        (string Rol, ClienteDePrueba Cliente)[] quienes =
        [
            ("DIRECTIVO", e.Directivo), ("ENTRENADOR", e.Entrenador), ("JUGADOR", await e.ClienteDeLaFamiliaAsync(e.Ana.Id)),
        ];

        foreach (var (rol, cliente) in quienes)
        {
            HttpResponseMessage[] respuestas =
            [
                await cliente.GetAsync(EnEspera(e)),
                await cliente.PostAsync(Aprobacion(e, hermano.Id)),
                await cliente.PostAsync(Rechazo(e, hermano.Id)),
            ];

            foreach (var respuesta in respuestas)
            {
                Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{rol}: {(int)respuesta.StatusCode}");
                Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        Assert.Equal(EstadoIngreso.EN_ESPERA, (await GuardadoAsync(e, hermano.Id)).EstadoIngreso);
    }

    // Escenario 3.6 y RF-019.
    [Fact]
    public async Task Tras_rechazarlo_la_familia_lo_agrega_de_nuevo_con_el_mismo_documento()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var cuerpo = EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez");
        var primero = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);
        var primerId = (await primero.JsonAsync()).GetProperty("usuarioRolId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await e.Presidente.PostAsync(Rechazo(e, primerId))).StatusCode);

        var deNuevo = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.Created, deNuevo.StatusCode);
        var nuevoId = (await deNuevo.JsonAsync()).GetProperty("usuarioRolId").GetGuid();
        Assert.NotEqual(primerId, nuevoId);
        var guardado = await GuardadoAsync(e, nuevoId);
        Assert.Equal((EstadoIngreso.EN_ESPERA, (string?)cuerpo["numeroDocumento"]), (guardado.EstadoIngreso, guardado.NumeroDocumento));
        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);
    }

    [Fact]
    public async Task Aprobar_y_rechazar_a_la_vez_deja_una_sola_de_las_dos_acciones()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var (otro, _) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE);
        var otroPresidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(otro);

        var respuestas = await Task.WhenAll(
            e.Presidente.PostAsync(Aprobacion(e, hermano.Id)), otroPresidente.PostAsync(Rechazo(e, hermano.Id)));
        var (aprobacion, rechazo) = (respuestas[0].StatusCode, respuestas[1].StatusCode);

        var jugadores = await e.JugadoresDeLaFamiliaAsync();
        if (aprobacion == HttpStatusCode.OK)
        {
            Assert.Equal(HttpStatusCode.Conflict, rechazo);
            Assert.Equal(EstadoIngreso.APROBADO, jugadores.Single(jugador => jugador.Id == hermano.Id).EstadoIngreso);
        }
        else
        {
            Assert.Equal(HttpStatusCode.NoContent, rechazo);
            Assert.Contains(aprobacion, new[] { HttpStatusCode.NotFound, HttpStatusCode.Conflict });
            Assert.Equal([e.Ana.Id], jugadores.Select(jugador => jugador.Id));
        }
    }

    // §14.1: el retiro es de cada jugador, no de la cuenta.
    [Fact]
    public async Task Retirar_a_un_hermano_no_cambia_al_otro()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Luis");
        var aprobado = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, nombres: "Mara", categoria: e.CategoriaDeAna);

        Assert.Equal(HttpStatusCode.NoContent, (await e.Presidente.PostAsync(e.Base.Retiro(e.Ana.Id))).StatusCode);

        Assert.False((await GuardadoAsync(e, e.Ana.Id)).Activo);
        var luis = await GuardadoAsync(e, enEspera.Id);
        var mara = await GuardadoAsync(e, aprobado.Id);
        Assert.Equal((EstadoIngreso.EN_ESPERA, true), (luis.EstadoIngreso, luis.Activo));
        Assert.Equal((EstadoIngreso.APROBADO, true, e.CategoriaDeAna.Id), (mara.EstadoIngreso, mara.Activo, mara.CategoriaId));

        // Y al revés: retirar al hermano no toca a nadie más, y el que está en espera se puede aprobar.
        Assert.Equal(HttpStatusCode.NoContent, (await e.Presidente.PostAsync(e.Base.Retiro(aprobado.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.Presidente.PostAsync(Aprobacion(e, enEspera.Id))).StatusCode);
        Assert.False((await GuardadoAsync(e, aprobado.Id)).Activo);
        luis = await GuardadoAsync(e, enEspera.Id);
        Assert.Equal((EstadoIngreso.APROBADO, true), (luis.EstadoIngreso, luis.Activo));

        var familia = await e.ClienteDeLaFamiliaAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await familia.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await familia.ElegirJugador(enEspera.Id).GetAsync(e.InicioDelClub)).StatusCode);
    }
}
