using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Ingresos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// A quien entra, es aprobado o se reincorpora se le cobra desde el mes siguiente: nunca el mes en
/// curso ni los anteriores (spec 015, historia 1; RF-008, escenarios 3 y 4).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PrimerMesDeCobroPruebas : IDisposable
{
    private static readonly DateOnly Diciembre = new(2026, 12, 1);

    private readonly FabricaApi _fabrica;

    public PrimerMesDeCobroPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private void FijarMitadDeNoviembre() =>
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 15), new TimeOnly(10, 0));

    private Task<UsuarioRol> GuardadoAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().SingleAsync(fila => fila.Id == usuarioRolId));

    private async Task AssertSinCargoEnNoviembreYConCargoEnDiciembreAsync(Club club, UsuarioRol jugador) =>
        await AssertCobraDesdeAsync(club, jugador, Diciembre);

    /// <summary>
    /// Comprueba que se le cobra desde ese mes: el mes anterior no tiene cargo y el día 1 de ese mes sí.
    /// </summary>
    private async Task AssertCobraDesdeAsync(Club club, UsuarioRol jugador, DateOnly primerMes)
    {
        Assert.Equal(primerMes, (await GuardadoAsync(jugador.Id)).CobraDesde);

        var anterior = primerMes.AddMonths(-1);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(anterior.Year, anterior.Month, 15), new TimeOnly(10, 0));
        await _fabrica.Pagos.GenerarAsync();
        Assert.DoesNotContain(await _fabrica.Pagos.CargosDelClubAsync(club), cargo => cargo.UsuarioRolId == jugador.Id);

        _fabrica.Reloj.FijarEnColombia(primerMes, new TimeOnly(0, 5));
        await _fabrica.Pagos.GenerarAsync();
        var cargo = Assert.Single(
            await _fabrica.Pagos.CargosDelClubAsync(club), fila => fila.UsuarioRolId == jugador.Id);
        Assert.Equal(primerMes, cargo.Mes);
    }

    [Fact]
    public async Task Un_ingreso_aprobado_se_cobra_desde_el_mes_siguiente()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var (_, enEspera) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(c.Club);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        FijarMitadDeNoviembre();

        var respuesta = await presidente.PostAsync(EscenarioIngresos.Aprobacion(c.Club, enEspera.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await AssertSinCargoEnNoviembreYConCargoEnDiciembreAsync(c.Club, enEspera);
    }

    [Fact]
    public async Task Un_hermano_agregado_se_cobra_desde_el_mes_siguiente_a_su_aprobacion()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var hermano = await _fabrica.Hermanos.CrearHermanoAsync(c.Ana.Integrante);
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        FijarMitadDeNoviembre();

        var respuesta = await presidente.PostAsync(EscenarioIngresos.Aprobacion(c.Club, hermano.Id));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await AssertSinCargoEnNoviembreYConCargoEnDiciembreAsync(c.Club, hermano);
    }

    [Fact]
    public async Task Quien_se_registra_con_una_invitacion_de_jugador_se_cobra_desde_el_mes_siguiente()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        // La invitación vence con el reloj real: se registra sin fijarlo.
        var (_, integrante) = await _fabrica.RegistrarConInvitacionAsync(c.Club, Rol.JUGADOR);

        await AssertCobraDesdeAsync(c.Club, integrante, ReglaMesDeCobro.PrimerMesDeCobro(DateTime.UtcNow));
    }

    [Fact]
    public async Task Quien_se_registra_con_otro_rol_no_tiene_fecha_de_cobro()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        var (_, entrenador) = await _fabrica.RegistrarConInvitacionAsync(c.Club, Rol.ENTRENADOR);

        Assert.Null((await GuardadoAsync(entrenador.Id)).CobraDesde);
    }

    [Fact]
    public async Task Un_jugador_reincorporado_se_cobra_desde_el_mes_siguiente()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var presidente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Presidente.Usuario);
        await _fabrica.Categorias.RetirarAsync(c.Beto.Integrante, c.Presidente.Integrante);
        FijarMitadDeNoviembre();

        var respuesta = await presidente.PostAsync(c.Ruta($"/jugadores/{c.Beto.Integrante.Id}/reincorporacion"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await AssertSinCargoEnNoviembreYConCargoEnDiciembreAsync(c.Club, c.Beto.Integrante);
    }
}
