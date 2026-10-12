using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Programar, cambiar, cancelar y reactivar un entrenamiento avisa a las familias de sus categorías,
/// una sola vez por cuenta y solo si algo cambió de verdad (spec 012, historia 1; RF-001, RF-004,
/// RF-005, RF-006, RF-010 y RF-026).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosDeEntrenamientoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosDeEntrenamientoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Programar_avisa_a_la_categoria_y_no_a_otra_ni_a_un_jugador_en_espera_o_retirado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var enEspera = await _fabrica.Hermanos.CrearHermanoAsync(e.Jn(6).Integrante, EstadoIngreso.EN_ESPERA, categoria: e.C2014);
        await _fabrica.Categorias.RetirarAsync(e.Jn(5).Integrante, e.P.Integrante);

        var creado = await Actividades.LeerAsync(
            await e.E2.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Created);

        var textos = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Single(textos);
        Assert.StartsWith("Nuevo entrenamiento", textos[0]);
        Assert.EndsWith("a las 16:00", textos[0]);
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1)); // 2015
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.M1)); // 2016
        Assert.Equal(0, await _fabrica.Avisos.ContarAsync(e.Jn(5).Cuenta)); // retirado: sin filas
        var avisos = await _fabrica.Avisos.DeLaActividadAsync(
            TipoDeActividad.ENTRENAMIENTO, creado.GetProperty("entrenamientoId").GetGuid());
        Assert.DoesNotContain(
            avisos.SelectMany(aviso => aviso.Destinatarios), fila => fila.UsuarioRolId == enEspera.Id);
    }

    [Fact]
    public async Task Un_entrenamiento_de_dos_categorias_avisa_una_sola_vez_a_la_cuenta_con_hermanos_en_las_dos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var creado = await Actividades.LeerAsync(
            await e.E2.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014, e.C2015])),
            HttpStatusCode.Created);

        // J1 (2014) y J2 (2015) son hermanos de la misma cuenta: un aviso, dos filas.
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(1)));
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(1)));
        var aviso = Assert.Single(await _fabrica.Avisos.DeLaActividadAsync(
            TipoDeActividad.ENTRENAMIENTO, creado.GetProperty("entrenamientoId").GetGuid()), a => a.Destinatarios.Any(
                fila => fila.UsuarioId == e.Jn(1).Cuenta.Id) && a.Destinatarios.Count > 0);
        Assert.Equal(2, aviso.Destinatarios.Count(fila => fila.UsuarioId == e.Jn(1).Cuenta.Id));
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }

    [Fact]
    public async Task Cambiar_hora_fecha_o_lugar_dice_que_cambio_y_como_queda()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo")));
        var porLugar = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Equal(["Cambió el lugar del entrenamiento: ahora en Coliseo"], porLugar);

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Entrenamiento(Manana, [e.C2014], horaInicio: "17:00", horaFin: "18:30", lugar: "Coliseo")));
        var porHora = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        // Una sola entrada sin leer por actividad: el aviso nuevo reemplazó al de lugar (RF-025).
        Assert.Equal(["Cambió la hora del entrenamiento: ahora mañana a las 17:00"], porHora);
    }

    [Fact]
    public async Task Guardar_lo_mismo_o_cambiar_solo_la_nota_no_avisa_a_nadie()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014])));
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            ruta, Actividades.Entrenamiento(Manana, [e.C2014], nota: "Otra nota distinta")));

        Assert.Empty(await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id));
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }

    [Fact]
    public async Task Cancelar_y_reactivar_avisan_y_el_aviso_abre_el_entrenamiento_cancelado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        var cancelado = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single();
        Assert.EndsWith("cancelado", cancelado.GetProperty("texto").GetString());
        Assert.True(cancelado.GetProperty("actividad").GetProperty("disponible").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await e.Jn(3).Cliente.GetAsync(ruta)).StatusCode);

        await Actividades.LeerAsync(await e.E1.Cliente.DeleteAsync($"{ruta}/cancelacion"));
        var textos = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Equal(["El entrenamiento de mañana sigue en pie"], textos);
    }

    [Fact]
    public async Task Un_fallo_de_validacion_o_un_409_no_deja_aviso_y_el_equipo_del_club_no_recibe_nada()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"));
        var antes = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id)).Count;

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PostAsync($"{ruta}/cancelacion"), HttpStatusCode.Conflict, "actividad_cancelada");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo")),
            HttpStatusCode.Conflict,
            "actividad_cancelada");
        var invalido = await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: ""));

        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Equal(antes, (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id)).Count);
        foreach (var equipo in new[] { e.P, e.E1, e.E2, e.D1 })
        {
            await Actividades.ErrorAsync(
                await equipo.Cliente.GetAsync(e.Ruta("/campana")), HttpStatusCode.Forbidden, "rol_no_autorizado");
            Assert.Equal(0, await _fabrica.Avisos.ContarAsync(equipo.Cuenta));
        }
    }
}
