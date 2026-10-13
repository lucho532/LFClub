using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Los avisos de un entrenamiento dirigido a equipos llegan solo a su audiencia: a los jugadores de
/// esos equipos y a los añadidos, una vez por cuenta; sin equipos llegan a toda la categoría como
/// antes (spec 013, historia 1; RF-024, RF-025; spec 012).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosDeAudienciaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosDeAudienciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Programar_para_un_equipo_avisa_solo_a_sus_jugadores_y_una_sola_vez_al_que_esta_en_dos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);

        await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [a, b])),
            HttpStatusCode.Created);
        await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana.AddDays(1), [e.C2014], equipos: [a])),
            HttpStatusCode.Created);

        Assert.Equal(2, (await AyudaDeAvisos.TextosAsync(e, e.Jn(3))).Count);   // solo A: los dos
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));              // solo B: el de A y B
        Assert.Equal(2, (await AyudaDeAvisos.TextosAsync(e, e.Jn(6))).Count);   // en A y B: uno por entrenamiento
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(7)));               // sin equipo
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));                  // otra categoría
    }

    [Fact]
    public async Task Programar_para_un_equipo_no_manda_aviso_al_jugador_del_otro_equipo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);

        await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [a])),
            HttpStatusCode.Created);

        Assert.NotEmpty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
        Assert.Equal(0, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(5)));
    }

    [Fact]
    public async Task Sin_equipos_recibe_toda_la_categoria_como_antes()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEquiposAsync(e);

        await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Created);

        foreach (var numero in new[] { 3, 5, 6, 7 })
        {
            Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(numero)));
        }

        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }

    [Fact]
    public async Task Cancelar_y_cambiar_un_entrenamiento_de_equipo_avisa_solo_a_ese_equipo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [a])),
            HttpStatusCode.Created);
        var ruta = e.Ruta($"/entrenamientos/{creado.GetProperty("entrenamientoId").GetGuid()}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo", equipos: [a])));
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));

        var deJ3 = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Single(deJ3);
        Assert.Contains("cancel", deJ3[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
    }

    [Fact]
    public async Task Cambiar_de_equipo_avisa_solo_a_quien_entra_y_no_deja_rastro_en_quien_sale()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [a])),
            HttpStatusCode.Created);
        var id = creado.GetProperty("entrenamientoId").GetGuid();
        var ruta = e.Ruta($"/entrenamientos/{id}");
        // J3 (solo A) y J6 (A y B) lo recibieron al programarlo; J5 (solo B), no.
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(6)));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
        await AyudaDeAvisos.AvisosAsync(e, e.Jn(6));
        var antesDeJ6 = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, id);

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/audiencia", new { equipoIds = new[] { b.Id }, jugadorIds = Array.Empty<Guid>() }));

        // J3 deja de verlo sin aviso y sin rastro en su campana.
        Assert.Equal(HttpStatusCode.NotFound, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
        Assert.Equal(0, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(3)));
        // J5 lo ve con un aviso nuevo.
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(5).Cliente.GetAsync(ruta)).StatusCode);
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(5)));
        // J6 estaba en los dos: sigue viéndolo y no recibe un aviso repetido.
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(6)));
        var despues = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, id);
        Assert.DoesNotContain(
            despues.SelectMany(aviso => aviso.Destinatarios), fila => fila.UsuarioRolId == e.Jn(3).Id);
        Assert.True(despues.SelectMany(aviso => aviso.Destinatarios).Count(fila => fila.UsuarioRolId == e.Jn(6).Id) <=
            antesDeJ6.SelectMany(aviso => aviso.Destinatarios).Count(fila => fila.UsuarioRolId == e.Jn(6).Id));
    }
}
