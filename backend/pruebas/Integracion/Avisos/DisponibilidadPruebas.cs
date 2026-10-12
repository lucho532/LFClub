using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Leer un aviso y saber si su actividad sigue disponible: <c>disponible</c> debe coincidir con que
/// el detalle real de la actividad responda 200 o 404 para esa misma cuenta, y el aviso nunca
/// abre nada que la cuenta no pueda ver (spec 012, historia 3; RF-017, RF-021 y RF-032).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DisponibilidadPruebas
{
    private static readonly DateOnly Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

    private readonly FabricaApi _fabrica;

    public DisponibilidadPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private Task<AvisoDeActividad> AvisarAsync(
        EscenarioActividades e, TipoDeActividad tipo, Guid actividadId, params UsuarioRol[] destinatarios) =>
        _fabrica.Avisos.GenerarAsync(
            e.Club,
            tipo == TipoDeActividad.ENTRENAMIENTO ? HechoDeActividad.ENTRENAMIENTO_CAMBIADO : HechoDeActividad.PARTIDO_CAMBIADO,
            tipo,
            actividadId,
            "Cambió el lugar: ahora en Cancha Minitas",
            destinatarios);

    private static string RutaDeActividad(EscenarioActividades e, TipoDeActividad tipo, Guid id) =>
        e.Ruta(tipo == TipoDeActividad.ENTRENAMIENTO ? $"/entrenamientos/{id}" : $"/partidos/{id}");

    private async Task<bool> LeerDisponibleAsync(EscenarioActividades e, Persona quien, AvisoDeActividad aviso)
    {
        var respuesta = await Actividades.LeerAsync(
            await quien.Cliente.PutAsync(e.Ruta($"/campana/avisos/{aviso.Id}/lectura"), new { }));
        var actividad = respuesta.GetProperty("actividad");
        Assert.Equal(aviso.ActividadId, actividad.GetProperty("id").GetGuid());
        Assert.Equal(aviso.ActividadTipo.ToString(), actividad.GetProperty("tipo").GetString());
        Assert.True(respuesta.GetProperty("leido").GetBoolean());
        return actividad.GetProperty("disponible").GetBoolean();
    }

    [Fact]
    public async Task Disponible_coincide_con_que_el_detalle_responda_200_o_404_para_esa_cuenta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var j4 = e.Jn(4);
        var entrenamientoDeSuCategoria = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Fecha);
        var entrenamientoDeOtra = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Fecha);
        var partidoConvocado = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Fecha, convocados: [j3.Integrante]);
        var partidoSinConvocar = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Fecha, convocados: [j4.Integrante]);
        var partidoJugado = await _fabrica.Actividades.CrearPartidoAsync([e.C2015], Fecha);
        await _fabrica.Actividades.RegistrarJugadoAsync(partidoJugado, 2, 1);

        var casos = new (TipoDeActividad Tipo, Guid Id, bool Esperado)[]
        {
            (TipoDeActividad.ENTRENAMIENTO, entrenamientoDeSuCategoria.Id, true),
            (TipoDeActividad.ENTRENAMIENTO, entrenamientoDeOtra.Id, false),
            (TipoDeActividad.PARTIDO, partidoConvocado.Id, true),
            (TipoDeActividad.PARTIDO, partidoSinConvocar.Id, false),
            (TipoDeActividad.PARTIDO, partidoJugado.Id, true),
        };

        foreach (var (tipo, id, esperado) in casos)
        {
            var aviso = await AvisarAsync(e, tipo, id, j3.Integrante);

            var disponible = await LeerDisponibleAsync(e, j3, aviso);
            var detalle = await j3.Cliente.GetAsync(RutaDeActividad(e, tipo, id));

            Assert.Equal(esperado, disponible);
            Assert.Equal(disponible ? HttpStatusCode.OK : HttpStatusCode.NotFound, detalle.StatusCode);
        }
    }

    [Fact]
    public async Task Un_partido_cancelado_sigue_abriendose_desde_el_aviso()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Fecha, convocados: [j3.Integrante], estado: EstadoPartido.CANCELADO);
        var aviso = await AvisarAsync(e, TipoDeActividad.PARTIDO, partido.Id, j3.Integrante);

        var disponible = await LeerDisponibleAsync(e, j3, aviso);
        var detalle = await j3.Cliente.GetAsync(RutaDeActividad(e, TipoDeActividad.PARTIDO, partido.Id));

        Assert.True(disponible);
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
    }

    [Fact]
    public async Task Quien_cambio_de_categoria_conserva_el_aviso_pero_la_actividad_ya_no_esta_disponible()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Fecha);
        var aviso = await AvisarAsync(e, TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id, j3.Integrante);
        await _fabrica.Actividades.CambiarCategoriaAsync(j3.Integrante, e.C2015);

        var disponible = await LeerDisponibleAsync(e, j3, aviso);
        var detalle = await j3.Cliente.GetAsync(RutaDeActividad(e, TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id));

        Assert.False(disponible);
        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);
    }

    [Fact]
    public async Task Leerlo_dos_veces_no_mueve_la_fecha_y_baja_el_contador_una_vez()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Fecha);
        var aviso = await AvisarAsync(e, TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id, j3.Integrante);
        var ruta = e.Ruta($"/campana/avisos/{aviso.Id}/lectura");

        var primera = await Actividades.LeerAsync(await j3.Cliente.PutAsync(ruta, new { }));
        var segunda = await Actividades.LeerAsync(await j3.Cliente.PutAsync(ruta, new { }));
        var contador = await Actividades.LeerAsync(await j3.Cliente.GetAsync(e.Ruta("/campana/contador")));

        Assert.Equal(primera.GetProperty("leidoEn").GetDateTime(), segunda.GetProperty("leidoEn").GetDateTime());
        Assert.Equal(0, contador.GetProperty("sinLeer").GetInt32());
    }

    [Fact]
    public async Task Leerlo_desde_un_hermano_lo_deja_leido_para_toda_la_cuenta()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j1 = e.Jn(1);
        var j2 = e.Jn(2);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Fecha);
        var aviso = await AvisarAsync(e, TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id, j1.Integrante, j2.Integrante);

        var respuesta = await Actividades.LeerAsync(
            await j1.Cliente.PutAsync(e.Ruta($"/campana/avisos/{aviso.Id}/lectura"), new { }));
        var contador = await Actividades.LeerAsync(await j1.Cliente.GetAsync(e.Ruta("/campana/contador")));

        Assert.True(respuesta.GetProperty("leido").GetBoolean());
        Assert.Equal(0, contador.GetProperty("sinLeer").GetInt32());
        Assert.All(
            await _fabrica.Avisos.LeerAsync(aviso.Id, j1.Cuenta),
            fila => Assert.NotNull(fila.LeidoEn));
    }

    [Fact]
    public async Task El_aviso_de_otra_cuenta_o_de_otro_club_responde_404_sin_distinguir()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Fecha);
        var deJ3 = await AvisarAsync(e, TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id, e.Jn(3).Integrante);

        // Otra cuenta del mismo club, un jugador de otro club, un identificador inventado y el desarrollador.
        await Actividades.ErrorAsync(
            await e.Jn(4).Cliente.PutAsync(e.Ruta($"/campana/avisos/{deJ3.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
        await Actividades.ErrorAsync(
            await e.JugadorB.Cliente.PutAsync(e.Ruta($"/campana/avisos/{deJ3.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PutAsync(e.Ruta($"/campana/avisos/{Guid.NewGuid()}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await Actividades.ErrorAsync(
            await desarrollador.PutAsync(e.Ruta($"/campana/avisos/{deJ3.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");
    }

    [Fact]
    public async Task Tener_el_identificador_de_un_aviso_o_de_una_actividad_no_da_acceso_a_la_actividad()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Fecha, convocados: [e.Jn(3).Integrante]);
        var aviso = await AvisarAsync(e, TipoDeActividad.PARTIDO, partido.Id, e.Jn(3).Integrante);

        // J4 no está convocado: ni el identificador del partido ni el del aviso le abren nada.
        var detalle = await e.Jn(4).Cliente.GetAsync(RutaDeActividad(e, TipoDeActividad.PARTIDO, partido.Id));
        await Actividades.ErrorAsync(
            await e.Jn(4).Cliente.PutAsync(e.Ruta($"/campana/avisos/{aviso.Id}/lectura"), new { }),
            HttpStatusCode.NotFound,
            "no_encontrado");

        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);
    }
}
