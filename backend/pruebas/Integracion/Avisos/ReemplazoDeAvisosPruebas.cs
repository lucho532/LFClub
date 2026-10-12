using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Una sola entrada sin leer por actividad en cada familia: varios cambios seguidos se reemplazan por
/// el último sin subir el contador, lo ya leído no se toca y un aviso sin destinatarios queda borrado
/// (spec 012, historia 4; RF-025, CE-005 y research §8).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ReemplazoDeAvisosPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ReemplazoDeAvisosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    private async Task CambiarLugarAsync(EscenarioActividades e, Guid entrenamientoId, string lugar) =>
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{entrenamientoId}"), Actividades.Entrenamiento(Manana, [e.C2014], lugar: lugar)));

    [Fact]
    public async Task Tres_cambios_seguidos_dejan_un_solo_aviso_sin_leer_con_el_texto_mas_reciente()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);

        await CambiarLugarAsync(e, entrenamiento.Id, "Coliseo");
        await CambiarLugarAsync(e, entrenamiento.Id, "Estadio");
        await CambiarLugarAsync(e, entrenamiento.Id, "Parque");

        foreach (var familia in new[] { e.Jn(3), e.Jn(4) })
        {
            Assert.Equal(["Cambió el lugar del entrenamiento: ahora en Parque"], await AyudaDeAvisos.TextosAsync(e, familia));
            Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, familia));
        }

        // Los reemplazados no quedan huérfanos: todo aviso guardado tiene al menos un destinatario.
        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        Assert.Single(avisos);
        Assert.All(avisos, aviso => Assert.NotEmpty(aviso.Destinatarios));
    }

    [Fact]
    public async Task Un_aviso_leido_no_se_toca_y_el_nuevo_se_suma_como_sin_leer()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        await CambiarLugarAsync(e, entrenamiento.Id, "Coliseo");
        var primero = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single();
        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync(
            e.Ruta($"/campana/avisos/{primero.GetProperty("id").GetGuid()}/lectura"), new { }));

        await CambiarLugarAsync(e, entrenamiento.Id, "Estadio");

        var avisos = await AyudaDeAvisos.AvisosAsync(e, e.Jn(3));
        Assert.Equal(2, avisos.Count);
        Assert.False(avisos[0].GetProperty("leido").GetBoolean());
        Assert.Contains("Estadio", avisos[0].GetProperty("texto").GetString());
        Assert.True(avisos[1].GetProperty("leido").GetBoolean());
        Assert.Contains("Coliseo", avisos[1].GetProperty("texto").GetString());
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(3)));
    }

    [Fact]
    public async Task Si_una_familia_lo_leyo_y_la_otra_no_la_otra_pierde_el_viejo_sin_que_la_primera_pierda_el_suyo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        await CambiarLugarAsync(e, entrenamiento.Id, "Coliseo");
        var viejo = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single();
        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync(
            e.Ruta($"/campana/avisos/{viejo.GetProperty("id").GetGuid()}/lectura"), new { }));

        await CambiarLugarAsync(e, entrenamiento.Id, "Estadio");

        // J3 lo había leído: conserva el viejo y suma el nuevo. J4 no: el viejo se reemplazó.
        Assert.Equal(2, (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Count);
        var deJ4 = await AyudaDeAvisos.TextosAsync(e, e.Jn(4));
        Assert.Equal(["Cambió el lugar del entrenamiento: ahora en Estadio"], deJ4);
        // El aviso viejo sigue guardado porque J3 aún lo tiene.
        var guardados = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        Assert.Equal(2, guardados.Count);
    }

    [Fact]
    public async Task Un_aviso_de_convocatoria_sin_leer_se_reemplaza_por_uno_de_cambio_de_lugar()
    {
        // Consecuencia aceptada (research §8): lo que pide CE-005; queda como documentación.
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana);
        var ruta = e.Ruta($"/partidos/{partido.Id}");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/convocatoria", new { usuarioRolIds = new[] { e.Jn(3).Id } }));
        Assert.Contains("fue convocado", (await AyudaDeAvisos.TextosAsync(e, e.Jn(3))).Single());

        var otroLugar = new
        {
            rival = "Equipo C",
            fecha = Actividades.Texto(Manana),
            hora = "09:00",
            lugar = "Estadio",
            condicion = "LOCAL",
            tipo = "AMISTOSO",
            nombreTorneo = (string?)null,
            categoriaIds = new[] { e.C2014.Id },
        };
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, otroLugar));

        var textos = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Equal(["Cambió el lugar del partido contra Equipo C: ahora en Estadio"], textos);
    }

    [Fact]
    public async Task Los_cambios_de_otra_actividad_no_reemplazan_y_cada_actividad_deja_su_propia_entrada()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var uno = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var otro = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana.AddDays(1));

        await CambiarLugarAsync(e, uno.Id, "Coliseo");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{otro.Id}"),
            Actividades.Entrenamiento(Manana.AddDays(1), [e.C2014], lugar: "Estadio")));

        var avisos = await AyudaDeAvisos.AvisosAsync(e, e.Jn(3));
        Assert.Equal(2, avisos.Count);
        Assert.Equal(2, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(3)));
    }
}
