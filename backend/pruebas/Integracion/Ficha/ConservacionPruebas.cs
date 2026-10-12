using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using LFClub.Pruebas.Integracion.Ingresos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ficha;

/// <summary>
/// El retiro conserva la ficha y sus archivos; borrar al jugador o al club los borra con él
/// (RF-036, RF-037; constitución §13 y §14.1).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConservacionPruebas
{
    private const DocumentoPedido Copia = DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD;

    private readonly FabricaApi _fabrica;

    public ConservacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // RF-036.
    [Fact]
    public async Task Retirar_y_reincorporar_a_un_jugador_conserva_su_ficha_y_sus_archivos()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        await e.CuentaDeAna.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());
        var archivo = SembradorFichas.Pdf(256);
        await e.CuentaDeAna.SubirAsync(e.Documento(e.Ana.Id, Copia), archivo);
        var fichaAntes = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        var comoLaDejo = (await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).GetProperty("datosClinicos").GetRawText();

        var retiro = await e.Presidente.PostAsync(e.Base.Retiro(e.Ana.Id));

        Assert.Equal(HttpStatusCode.NoContent, retiro.StatusCode);
        var fichaRetirado = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        Assert.Equal(fichaAntes.Alergias, fichaRetirado.Alergias);
        Assert.Equal(fichaAntes.UltimoCambioEn, fichaRetirado.UltimoCambioEn);
        Assert.Equal(archivo, Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id)).Contenido);
        Assert.Equal(HttpStatusCode.Forbidden, (await e.CuentaDeAna.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Entrenador.GetAsync(e.Ficha(e.Ana.Id))).StatusCode);
        foreach (var cliente in new[] { e.Presidente, e.Directivo })
        {
            var ficha = await e.FichaDeAsync(cliente, e.Ana.Id);
            Assert.True(ficha.GetProperty("retirado").GetBoolean());
            Assert.Equal("Nueva EPS", ficha.GetProperty("seguridadSocial").GetProperty("entidadSalud").GetString());
            Assert.Equal(archivo, await (await cliente.GetAsync(e.Documento(e.Ana.Id, Copia))).Content.ReadAsByteArrayAsync());
        }

        var reincorporacion = await e.Presidente.PostAsync(e.Base.Reincorporacion(e.Ana.Id));

        Assert.Equal(HttpStatusCode.OK, reincorporacion.StatusCode);
        var alVolver = await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id);
        Assert.Equal(comoLaDejo, alVolver.GetProperty("datosClinicos").GetRawText());
        Assert.True(alVolver.DocumentoDe(Copia).GetProperty("entregado").GetBoolean());
        Assert.Equal(fichaAntes.UltimoCambioEn, (await e.FichaGuardadaAsync(e.Ana.Id))!.UltimoCambioEn);
    }

    // RF-037: rechazar a quien espera borra sus datos del club, también los de su ficha.
    [Fact]
    public async Task Rechazar_a_un_jugador_en_espera_no_deja_ninguna_fila_de_su_ficha_ni_de_sus_archivos()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(e.Club);
        await SembrarAsync(enEspera);
        await SembrarAsync(e.Ana);

        var rechazo = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, enEspera.Id));

        Assert.True(rechazo.IsSuccessStatusCode, $"rechazo: {(int)rechazo.StatusCode}");
        Assert.Null(await e.FichaGuardadaAsync(enEspera.Id));
        Assert.Empty(await e.DocumentosGuardadosAsync(enEspera.Id));
        Assert.NotNull(await e.FichaGuardadaAsync(e.Ana.Id));
        Assert.Single(await e.DocumentosGuardadosAsync(e.Ana.Id));
    }

    // RF-037: eliminar el club se lleva las fichas y los archivos; los de otro club quedan intactos.
    [Fact]
    public async Task Eliminar_el_club_no_deja_ninguna_fila_suya_y_las_de_otro_club_quedan_intactas()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (_, ajeno) = await e.Base.Sembrar.CrearJugadorAsync(e.Base.OtroClub, 2014);
        await SembrarAsync(e.Ana);
        await SembrarAsync(e.Dani);
        await SembrarAsync(ajeno);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await desarrollador.PutAsync($"/api/plataforma/clubes/{e.Club.Id}/estado", new { estado = "DADO_DE_BAJA" });

        var eliminacion = await desarrollador.PostAsync(
            $"/api/plataforma/clubes/{e.Club.Id}/eliminacion", new { nombreDeConfirmacion = e.Club.Nombre });

        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);
        Assert.Equal((0, 0), await FilasDelClubAsync(e.Club.Id));
        Assert.Equal((1, 1), await FilasDelClubAsync(e.Base.OtroClub.Id));
        Assert.Equal(SembradorFichas.Alergias, (await e.FichaGuardadaAsync(ajeno.Id))!.Alergias);
    }

    // §13: el nombre copiado se conserva y la referencia queda nula.
    [Fact]
    public async Task Eliminar_la_cuenta_de_quien_hizo_el_ultimo_cambio_conserva_su_nombre_copiado()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);
        var (cuentaOtroPresidente, otroPresidente) = await _fabrica.Sembrador.CrearIntegranteAsync(e.Club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaOtroPresidente);
        await cliente.PutAsync(e.Ficha(e.Ana.Id), EscenarioFicha.Cuerpo());
        Assert.Equal(cuentaOtroPresidente.Id, (await e.FichaGuardadaAsync(e.Ana.Id))!.UltimoCambioPorUsuarioId);

        await _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.UsuariosRol.IgnoreQueryFilters().Where(integrante => integrante.Id == otroPresidente.Id).ExecuteDeleteAsync();
            return await contexto.Usuarios.Where(usuario => usuario.Id == cuentaOtroPresidente.Id).ExecuteDeleteAsync();
        });

        var guardada = (await e.FichaGuardadaAsync(e.Ana.Id))!;
        Assert.Null(guardada.UltimoCambioPorUsuarioId);
        Assert.Equal($"{otroPresidente.Nombres} {otroPresidente.Apellidos}", guardada.UltimoCambioPorNombre);
        var cambio = (await e.FichaDeAsync(e.CuentaDeAna, e.Ana.Id)).GetProperty("ultimoCambio");
        Assert.Equal(guardada.UltimoCambioPorNombre, cambio.GetProperty("autor").GetString());
        Assert.False(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
    }

    private async Task SembrarAsync(UsuarioRol jugador)
    {
        await _fabrica.Fichas.CrearFichaAsync(jugador);
        await _fabrica.Fichas.CrearDocumentoAsync(jugador, Copia, SembradorFichas.Pdf(), "application/pdf");
    }

    private Task<(int Fichas, int Documentos)> FilasDelClubAsync(Guid clubId) => _fabrica.ConContextoAsync(async contexto => (
        await contexto.FichasJugador.IgnoreQueryFilters().CountAsync(ficha => ficha.ClubId == clubId),
        await contexto.DocumentosJugador.IgnoreQueryFilters().CountAsync(archivo => archivo.ClubId == clubId)));
}
