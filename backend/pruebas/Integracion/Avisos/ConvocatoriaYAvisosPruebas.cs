using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Avisos;

/// <summary>
/// Convocar avisa a la familia del jugador convocado, con un solo aviso por cuenta que lo nombra;
/// sacar a alguien de la convocatoria no avisa a nadie (spec 012, historia 2; RF-002, RF-015, RF-021
/// y research §9).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConvocatoriaYAvisosPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ConvocatoriaYAvisosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    private static object Convocar(params Persona[] jugadores) =>
        new { usuarioRolIds = jugadores.Select(jugador => jugador.Id) };

    [Fact]
    public async Task Convocar_a_un_jugador_avisa_solo_a_su_familia_con_su_nombre()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3))));

        var textos = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Equal(["J3 fue convocado al partido contra Equipo C mañana a las 9:00"], textos);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(4)));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }

    [Fact]
    public async Task Dos_hermanos_en_un_guardado_reciben_un_solo_aviso_que_nombra_a_los_dos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");

        await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(ruta, Convocar(e.Jn(1), e.Jn(2))));

        var avisos = await AyudaDeAvisos.AvisosAsync(e, e.Jn(1));
        var aviso = Assert.Single(avisos);
        Assert.Equal(
            "J1 y J2 fueron convocados al partido contra Equipo C mañana a las 9:00",
            aviso.GetProperty("texto").GetString());
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(1)));
    }

    [Fact]
    public async Task Una_sesion_limitada_al_documento_de_un_hermano_no_ve_el_aviso_del_otro()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014, e.C2015], Manana);
        await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(
            e.Ruta($"/partidos/{partido.Id}/convocatoria"), Convocar(e.Jn(1))));

        var limitadaAJ2 = await _fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(e.Jn(1).Cuenta, e.Jn(2).Id);
        var limitadaAJ1 = await _fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(e.Jn(1).Cuenta, e.Jn(1).Id);

        var deJ2 = await Actividades.LeerAsync(await limitadaAJ2.GetAsync(e.Ruta("/campana")));
        var deJ1 = await Actividades.LeerAsync(await limitadaAJ1.GetAsync(e.Ruta("/campana")));
        Assert.Empty(deJ2.GetProperty("elementos").EnumerateArray());
        Assert.Single(deJ1.GetProperty("elementos").EnumerateArray());
    }

    [Fact]
    public async Task Sacar_a_un_jugador_no_avisa_a_nadie_y_el_aviso_sin_leer_que_tenia_dice_que_ya_no_esta_disponible()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3), e.Jn(4))));
        var antes = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id)).Count;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(4))));

        Assert.Equal(antes, (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id)).Count);
        // J3 conserva su aviso sin leer; al abrirlo el servidor dice que la actividad ya no está disponible.
        var deJ3 = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single();
        Assert.False(deJ3.GetProperty("actividad").GetProperty("disponible").GetBoolean());
        var leido = await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync(
            e.Ruta($"/campana/avisos/{deJ3.GetProperty("id").GetGuid()}/lectura"), new { }));
        Assert.True(leido.GetProperty("leido").GetBoolean());
        Assert.False(leido.GetProperty("actividad").GetProperty("disponible").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await e.Jn(3).Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).StatusCode);
    }

    [Fact]
    public async Task Guardar_la_misma_convocatoria_o_convocar_a_quien_no_es_de_la_categoria_no_avisa()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/partidos/{partido.Id}/convocatoria");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3))));
        var antes = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id)).Count;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3))));
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Convocar(e.Jn(3), e.K1)),
            HttpStatusCode.Conflict,
            "jugador_fuera_de_la_actividad");

        Assert.Equal(antes, (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id)).Count);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }
}
