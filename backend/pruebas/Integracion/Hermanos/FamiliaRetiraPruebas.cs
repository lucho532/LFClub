using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Hermanos;

/// <summary>
/// La cuenta de un jugador lo retira del club con la operación de retiro de la 003, solo al jugador
/// de la petición; la cuenta y sus demás jugadores no cambian, y la familia no reincorpora
/// (historia 3, escenarios 14 y 16; RF-042 y RF-043; research §18 y §20 de la 006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FamiliaRetiraPruebas
{
    private readonly FabricaApi _fabrica;

    public FamiliaRetiraPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Retiro(EscenarioHermanos e, Guid id) => $"/api/clubes/{e.Club.Id}/jugadores/{id}/retiro";

    private Task<FichaJugador> FichaGuardadaAsync(Guid id) => _fabrica.ConContextoAsync(contexto =>
        contexto.FichasJugador.IgnoreQueryFilters().AsNoTracking().SingleAsync(fila => fila.UsuarioRolId == id));

    // Escenario 3.14 y RF-042.
    [Fact]
    public async Task Con_su_jugador_elegido_lo_retira_y_la_cuenta_y_sus_hermanos_no_cambian()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, categoria: e.CategoriaDeAna);
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(e.CategoriaDeAna, "A");
        await _fabrica.Categorias.PonerEnEquipoAsync(e.Ana, equipo);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");
        var cuentaAntes = await e.FamiliaGuardadaAsync();
        var fichaAntes = await FichaGuardadaAsync(e.Ana.Id);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        var respuesta = await familia.PostAsync(Retiro(e, e.Ana.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var jugadores = await e.JugadoresDeLaFamiliaAsync();
        var ana = jugadores.Single(jugador => jugador.Id == e.Ana.Id);
        Assert.False(ana.Activo);
        Assert.Null(ana.CategoriaId);
        Assert.Empty(ana.Equipos);
        Assert.Equal(e.Familia.Id, ana.RetiradoPorUsuarioId);
        Assert.Equal($"{e.Ana.Nombres} {e.Ana.Apellidos}", ana.RetiradoPorNombre);
        var otro = jugadores.Single(jugador => jugador.Id == luis.Id);
        Assert.Equal((true, e.CategoriaDeAna.Id), (otro.Activo, otro.CategoriaId));

        var cuentaDespues = await e.FamiliaGuardadaAsync();
        Assert.Equal(
            (cuentaAntes.Correo, cuentaAntes.Celular, cuentaAntes.NombreResponsable, cuentaAntes.ContrasenaHash),
            (cuentaDespues.Correo, cuentaDespues.Celular, cuentaDespues.NombreResponsable, cuentaDespues.ContrasenaHash));
        Assert.Equal(fichaAntes.UltimoCambioEn, (await FichaGuardadaAsync(e.Ana.Id)).UltimoCambioEn);

        // Ya no entra al club con ella, y el presidente la ve retirada por su propio nombre.
        var despues = await familia.GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.Forbidden, despues.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(despues));
        var retirada = (await e.Presidente.ListaAsync($"/api/clubes/{e.Club.Id}/jugadores/retirados"))
            .Single(jugador => jugador.GetProperty("usuarioRolId").GetGuid() == e.Ana.Id);
        Assert.Equal($"{e.Ana.Nombres} {e.Ana.Apellidos}", retirada.GetProperty("retiradoPor").GetString());
    }

    // Escenario 3.16 y RF-043.
    [Fact]
    public async Task No_retira_a_un_hermano_no_elegido_ni_a_un_jugador_ajeno_y_no_reincorpora()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        foreach (var ajeno in new[] { luis.Id, e.Beto.Id })
        {
            var respuesta = await familia.PostAsync(Retiro(e, ajeno));
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.All(await e.JugadoresDeLaFamiliaAsync(), jugador => Assert.True(jugador.Activo));

        // Retirada Ana, la familia no la reincorpora aunque continúe con Luis.
        Assert.Equal(HttpStatusCode.NoContent, (await familia.PostAsync(Retiro(e, e.Ana.Id))).StatusCode);
        var conLuis = await e.ClienteDeLaFamiliaAsync(luis.Id);
        var reincorporar = await conLuis.PostAsync($"/api/clubes/{e.Club.Id}/jugadores/{e.Ana.Id}/reincorporacion");
        Assert.Equal(HttpStatusCode.Forbidden, reincorporar.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(reincorporar));
    }

    [Fact]
    public async Task En_espera_no_se_retira_y_con_la_sesion_del_documento_si()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana);

        var desdeLaEspera = await (await e.ClienteDeLaFamiliaAsync(enEspera.Id)).PostAsync(Retiro(e, enEspera.Id));
        Assert.Equal(HttpStatusCode.Forbidden, desdeLaEspera.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(desdeLaEspera));

        var limitada = await e.ClienteLimitadoAsync(e.Ana.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await limitada.PostAsync(Retiro(e, e.Ana.Id))).StatusCode);
    }

    [Fact]
    public async Task El_directivo_y_el_entrenador_no_retiran()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);

        foreach (var cliente in new[] { e.Directivo, e.Entrenador })
        {
            var respuesta = await cliente.PostAsync(Retiro(e, e.Ana.Id));
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
        }
    }
}
