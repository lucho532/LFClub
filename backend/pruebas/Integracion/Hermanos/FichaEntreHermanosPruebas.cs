using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using LaPecosa.Pruebas.Integracion.Ficha;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// La ficha y sus archivos son del jugador elegido y de ningún otro, tampoco de un hermano de la
/// misma cuenta; el contacto, que es de la cuenta, lo comparten (historia 2, escenario 7; RF-030;
/// CE-008; RF-039 de la 005). La 005 no tiene operación para borrar un documento: se reemplaza.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaEntreHermanosPruebas
{
    private const DocumentoPedido Certificado = DocumentoPedido.CERTIFICADO_SALUD;

    private readonly FabricaApi _fabrica;

    public FichaEntreHermanosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Ficha(EscenarioHermanos e, UsuarioRol jugador) => EscenarioFicha.Ficha(e.Club.Id, jugador.Id);

    private static string Documento(EscenarioHermanos e, UsuarioRol jugador) => $"{Ficha(e, jugador)}/documentos/{Certificado}";

    private static object NuevoDocumento() =>
        new { tipoDocumento = "TARJETA_IDENTIDAD", numeroDocumento = Sembrador.Unico("ti") };

    private static object Identidad() => new { nombres = "Otro", apellidos = "Nombre", fechaNacimiento = "2013-01-01" };

    /// <summary>Luis, hermano aprobado de Ana en su misma categoría, los dos con ficha y certificado.</summary>
    private async Task<(EscenarioHermanos E, UsuarioRol Luis)> ConFichasAsync()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, categoria: e.CategoriaDeAna);
        foreach (var jugador in new[] { e.Ana, luis })
        {
            await _fabrica.Fichas.CrearFichaAsync(jugador);
            await _fabrica.Fichas.CrearDocumentoAsync(jugador, Certificado, SembradorFichas.Pdf(64), "application/pdf");
        }

        return (e, luis);
    }

    private Task<UsuarioRol> GuardadoAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking().FirstAsync(integrante => integrante.Id == usuarioRolId));

    private Task<FichaJugador?> FichaGuardadaAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.FichasJugador.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(ficha => ficha.UsuarioRolId == usuarioRolId));

    // Escenario 2.7 y CE-008.
    [Fact]
    public async Task Con_un_hijo_elegido_las_operaciones_sobre_la_ficha_de_su_hermano_responden_404_y_nada_cambia()
    {
        var (e, luis) = await ConFichasAsync();
        var conAna = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var inexistente = await conAna.GetAsync(EscenarioFicha.Ficha(e.Club.Id, Guid.NewGuid()));

        var respuestas = new Dictionary<string, HttpResponseMessage>
        {
            ["GET ficha"] = await conAna.GetAsync(Ficha(e, luis)),
            ["PUT ficha"] = await conAna.PutAsync(Ficha(e, luis), EscenarioFicha.Cuerpo()),
            ["PUT documento-identidad"] = await conAna.PutAsync($"{Ficha(e, luis)}/documento-identidad", NuevoDocumento()),
            ["GET documento"] = await conAna.GetAsync(Documento(e, luis)),
            ["PUT documento"] = await conAna.SubirAsync(Documento(e, luis), SembradorFichas.Jpeg(), "certificado.jpg"),
        };

        foreach (var (operacion, respuesta) in respuestas)
        {
            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{operacion}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await respuesta.Content.ReadAsStringAsync());
        }

        // Corregir la identidad es solo del PRESIDENTE: a la familia se le niega por el rol, también en la suya.
        var identidad = await conAna.PutAsync($"{Ficha(e, luis)}/identidad", Identidad());
        Assert.Equal(HttpStatusCode.Forbidden, identidad.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(identidad));

        var guardado = await GuardadoAsync(luis.Id);
        Assert.Equal((luis.Nombres, luis.NumeroDocumento, luis.FechaNacimiento), (guardado.Nombres, guardado.NumeroDocumento, guardado.FechaNacimiento));
        Assert.Equal(SembradorFichas.EntidadSalud, (await FichaGuardadaAsync(luis.Id))!.EntidadSalud);
        Assert.Equal("3001234567", (await e.FamiliaGuardadaAsync()).Celular);
        var archivo = await _fabrica.ConContextoAsync(contexto => contexto.DocumentosJugador.IgnoreQueryFilters()
            .AsNoTracking().SingleAsync(documento => documento.UsuarioRolId == luis.Id));
        Assert.Equal("application/pdf", archivo.TipoContenido);
    }

    [Fact]
    public async Task Con_ese_hermano_elegido_las_mismas_operaciones_responden_como_a_su_familia()
    {
        var (e, luis) = await ConFichasAsync();
        var conLuis = await e.ClienteDeLaFamiliaAsync(luis.Id);

        var lectura = await conLuis.GetAsync(Ficha(e, luis));
        var cambio = await conLuis.PutAsync(Ficha(e, luis), EscenarioFicha.Cuerpo());
        var documento = await conLuis.PutAsync($"{Ficha(e, luis)}/documento-identidad", NuevoDocumento());
        var apertura = await conLuis.GetAsync(Documento(e, luis));
        var subida = await conLuis.SubirAsync(Documento(e, luis), SembradorFichas.Jpeg(), "certificado.jpg");
        var identidad = await conLuis.PutAsync($"{Ficha(e, luis)}/identidad", Identidad());

        Assert.All(
            new[] { lectura, cambio, documento, apertura, subida },
            respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
        Assert.Equal(luis.Id, (await lectura.JsonAsync()).GetProperty("usuarioRolId").GetGuid());
        Assert.True((await lectura.JsonAsync()).GetProperty("permisos").GetProperty("puedeCambiar").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, identidad.StatusCode);

        // Y con Luis elegido, la de Ana es la que no existe.
        Assert.Equal(HttpStatusCode.NotFound, (await conLuis.GetAsync(Ficha(e, e.Ana))).StatusCode);
    }

    [Fact]
    public async Task Lo_guardado_en_la_ficha_de_uno_no_aparece_en_la_del_otro_y_el_contacto_es_de_los_dos()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        var cambio = await familia.PutAsync(Ficha(e, e.Ana), EscenarioFicha.Cuerpo());
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);

        var deAna = await (await familia.GetAsync(Ficha(e, e.Ana))).JsonAsync();
        var deLuis = await (await familia.ElegirJugador(luis.Id).GetAsync(Ficha(e, luis))).JsonAsync();

        // La salud y la emergencia son de cada jugador.
        Assert.Equal("Polen", deAna.GetProperty("datosClinicos").GetProperty("alergias").GetString());
        Assert.Equal(JsonValueKind.Null, deLuis.GetProperty("datosClinicos").GetProperty("alergias").ValueKind);
        Assert.Equal(JsonValueKind.Null, deLuis.GetProperty("seguridadSocial").GetProperty("entidadSalud").ValueKind);
        Assert.Null(await FichaGuardadaAsync(luis.Id));

        // El celular y el responsable son de la cuenta (RF-039 de la 005): se leen en la del hermano.
        foreach (var ficha in new[] { deAna, deLuis })
        {
            Assert.Equal(EscenarioFicha.CelularNuevo, ficha.GetProperty("contacto").GetProperty("celular").GetString());
            Assert.Equal(
                EscenarioFicha.ResponsableNuevo, ficha.GetProperty("contacto").GetProperty("nombreResponsable").GetString());
        }

        // El último cambio queda solo en la ficha desde la que se hizo.
        Assert.True(deAna.Tiene("ultimoCambio"));
        Assert.False(deLuis.Tiene("ultimoCambio"));
    }

    [Fact]
    public async Task El_presidente_el_directivo_y_el_entrenador_ven_las_dos_fichas_sin_cabecera()
    {
        var (e, luis) = await ConFichasAsync();
        (string Rol, ClienteDePrueba Cliente, bool Clinicos, bool Documentos)[] quienes =
        [
            ("PRESIDENTE", e.Presidente, true, true),
            ("DIRECTIVO", e.Directivo, false, true),
            ("ENTRENADOR", e.Entrenador, true, false),
        ];

        foreach (var (rol, cliente, clinicos, documentos) in quienes)
        {
            foreach (var jugador in new[] { e.Ana, luis })
            {
                var respuesta = await cliente.GetAsync(Ficha(e, jugador));

                Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{rol}: {(int)respuesta.StatusCode}");
                var ficha = await respuesta.JsonAsync();
                Assert.Equal(jugador.Id, ficha.GetProperty("usuarioRolId").GetGuid());
                Assert.Equal(clinicos, ficha.Tiene("datosClinicos"));
                Assert.Equal(documentos, ficha.Tiene("documentos"));
            }
        }
    }
}
