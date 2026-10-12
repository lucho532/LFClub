using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Programar un entrenamiento y consultarlo: lo programa el PRESIDENTE o quien entrena todas sus
/// categorías, con categorías activas del club; y nadie que no lo ve llega a él (historia 1,
/// escenarios 1.1 a 1.4 y 1.8; RF-001, RF-002, RF-005, RF-011).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ProgramarEntrenamientoPruebas
{
    private readonly FabricaApi _fabrica;

    public ProgramarEntrenamientoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Quien_entrena_las_dos_categorias_programa_uno_conjunto_sin_mover_a_nadie_de_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var respuesta = await e.E2.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014, e.C2015]));

        var creado = await Actividades.LeerAsync(respuesta, HttpStatusCode.Created);
        Assert.Equal("PROGRAMADO", creado.GetProperty("estado").GetString());
        Assert.Equal([2014, 2015], Actividades.Anios(creado));
        Assert.Equal("16:00", creado.GetProperty("horaInicio").GetString());
        Assert.Equal("Traer peto", creado.GetProperty("nota").GetString());
        Assert.True(creado.GetProperty("permisos").GetProperty("gestiona").GetBoolean());

        // Escenario 1.2: dirigirlo a dos categorías no cambia la categoría de nadie.
        var categorias = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.Id == e.Jn(3).Id || integrante.Id == e.K1.Id)
            .ToDictionaryAsync(integrante => integrante.Id, integrante => integrante.CategoriaId));
        Assert.Equal(e.C2014.Id, categorias[e.Jn(3).Id]);
        Assert.Equal(e.C2015.Id, categorias[e.K1.Id]);
    }

    [Fact]
    public async Task El_presidente_programa_para_una_categoria_que_no_entrena_y_la_marca_como_usada()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var para2016 = await e.P.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2016]));
        var para2013 = await e.P.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2013]));

        Assert.Equal(HttpStatusCode.Created, para2016.StatusCode);
        Assert.Equal(HttpStatusCode.Created, para2013.StatusCode);
        Assert.True(await _fabrica.ConContextoAsync(contexto => contexto.Categorias.IgnoreQueryFilters()
            .Where(categoria => categoria.Id == e.C2013.Id).Select(categoria => categoria.Usada).SingleAsync()));
    }

    [Fact]
    public async Task Quien_no_entrena_todas_las_categorias_recibe_no_gestiona_actividad()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var ruta = e.Ruta("/entrenamientos");

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PostAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014, e.C2015])),
            HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PostAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2016])),
            HttpStatusCode.Forbidden, "no_gestiona_actividad");
        await Actividades.ErrorAsync(
            await e.D2.Cliente.PostAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Forbidden, "no_gestiona_actividad");
        Assert.Equal(HttpStatusCode.Created, (await e.D1.Cliente.PostAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014]))).StatusCode);
        await Actividades.ErrorAsync(
            await e.Jn(3).Cliente.PostAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Forbidden, "rol_no_autorizado");
    }

    [Fact]
    public async Task Los_datos_que_no_son_validos_se_rechazan_con_el_error_en_su_campo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var ruta = e.Ruta("/entrenamientos");
        var casos = new (object Cuerpo, string Campo)[]
        {
            (Actividades.Entrenamiento(Manana, [e.C2014], horaInicio: "17:30", horaFin: "16:00"), "horaFin"),
            (Actividades.Entrenamiento(Manana, []), "categoriaIds"),
            (Actividades.Entrenamiento(Manana, [e.C2014], lugar: "   "), "lugar"),
            (Actividades.Entrenamiento(Manana, [e.C2014], nota: new string('n', 501)), "nota"),
            (Actividades.Entrenamiento(Manana, [e.C2014], horaInicio: "25:00"), "horaInicio"),
        };

        foreach (var (cuerpo, campo) in casos)
        {
            var respuesta = await e.E1.Cliente.PostAsync(ruta, cuerpo);

            await Actividades.ErrorAsync(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
            Assert.True((await Actividades.LeerAsync(respuesta, HttpStatusCode.BadRequest))
                .GetProperty("errores").TryGetProperty(campo, out _), campo);
        }
    }

    [Fact]
    public async Task Una_categoria_desactivada_o_de_otro_club_responde_categoria_inactiva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var inactiva = await _fabrica.Categorias.CrearCategoriaAsync(e.Club, 2012, activa: false);

        foreach (var categoria in new[] { inactiva, e.CB })
        {
            await Actividades.ErrorAsync(
                await e.P.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [categoria])),
                HttpStatusCode.Conflict, "categoria_inactiva");
        }
    }

    [Fact]
    public async Task Quien_no_lo_ve_recibe_el_mismo_404_que_por_un_identificador_inventado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Manana);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        // Escenario 1.8: desde el club B, con la ruta de A o con la suya.
        await Actividades.ErrorAsync(await e.PB.Cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.PB.Cliente.GetAsync(e.RutaB($"/entrenamientos/{entrenamiento.Id}")), HttpStatusCode.NotFound, "no_encontrado");

        var deM1 = await e.M1.Cliente.GetAsync(ruta);
        var inventado = await e.M1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{Guid.NewGuid()}"));
        await Actividades.ErrorAsync(deM1, HttpStatusCode.NotFound, "no_encontrado");
        Assert.Equal(await inventado.Content.ReadAsStringAsync(), await deM1.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_jugador_recibe_el_detalle_sin_respuesta_ni_asistencia_de_nadie()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);

        var respuesta = await e.Jn(3).Cliente.GetAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}"));

        var detalle = await Actividades.LeerAsync(respuesta);
        var texto = detalle.GetRawText();
        Assert.DoesNotContain("miRespuesta", texto);
        Assert.DoesNotContain("miAsistencia", texto);
        Assert.False(detalle.GetProperty("permisos").GetProperty("gestiona").GetBoolean());
        Assert.True(detalle.GetProperty("permisos").GetProperty("responde").GetBoolean());
    }
}
