using System.Net;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Programar un entrenamiento para equipos: validación de los equipos, varios días a la vez, que el
/// cambio de datos no cambie la audiencia, responder y registrar asistencia por equipo, y que un
/// entrenamiento sin equipos se comporte como antes (spec 013, historia 1; RF-001 a RF-008, RF-025;
/// CE-005).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EntrenamientoPorEquipoPruebas : IDisposable
{
    private static readonly object Asistira = new { respuesta = "ASISTIRA" };

    private readonly FabricaApi _fabrica;

    public EntrenamientoPorEquipoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Programar_con_un_equipo_lo_guarda_lo_marca_usado_y_lo_devuelve_con_su_equipo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);

        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [a])),
            HttpStatusCode.Created);

        var equipos = creado.GetProperty("equipos").EnumerateArray().ToList();
        Assert.Equal(a.Id, Assert.Single(equipos).GetProperty("equipoId").GetGuid());
        var usado = await _fabrica.ConContextoAsync(contexto =>
            contexto.Equipos.IgnoreQueryFilters().Where(equipo => equipo.Id == a.Id).Select(equipo => equipo.Usado).SingleAsync());
        Assert.True(usado);
    }

    [Fact]
    public async Task Sin_equipos_es_de_toda_la_categoria_y_se_comporta_como_antes()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEquiposAsync(e);

        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])),
            HttpStatusCode.Created);
        var id = creado.GetProperty("entrenamientoId").GetGuid();

        Assert.Equal(0, creado.GetProperty("equipos").GetArrayLength());
        foreach (var numero in new[] { 3, 5, 6, 7 })
        {
            Assert.Equal(HttpStatusCode.OK, (await e.Jn(numero).Cliente.GetAsync(e.Ruta($"/entrenamientos/{id}"))).StatusCode);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await e.K1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{id}"))).StatusCode);
    }

    [Fact]
    public async Task Un_equipo_de_otra_categoria_inactivo_o_de_otro_club_se_rechaza_con_equipo_no_valido()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var deLa2015 = await _fabrica.Categorias.CrearEquipoAsync(e.C2015, "A");
        var inactivo = await _fabrica.Categorias.CrearEquipoAsync(e.C2014, "Viejo", activo: false);
        var deOtroClub = await _fabrica.Categorias.CrearEquipoAsync(e.CB, "A");

        foreach (var equipo in new[] { deLa2015, inactivo, deOtroClub })
        {
            await Actividades.ErrorAsync(
                await e.P.Cliente.PostAsync(
                    e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [equipo])),
                HttpStatusCode.UnprocessableEntity,
                "equipo_no_valido");
        }
    }

    [Fact]
    public async Task Un_equipo_de_una_categoria_inactiva_se_rechaza()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(e.Club, 2012, activa: false);
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(categoria, "A");

        var respuesta = await e.P.Cliente.PostAsync(
            e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [categoria], equipos: [equipo]));

        Assert.True(respuesta.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Programar_varios_dias_con_los_mismos_equipos_y_cambiar_uno_no_cambia_los_demas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var ids = new List<Guid>();

        foreach (var dia in new[] { Manana, Manana.AddDays(2), Manana.AddDays(4) })
        {
            var creado = await Actividades.LeerAsync(
                await e.E1.Cliente.PostAsync(
                    e.Ruta("/entrenamientos"), Actividades.Entrenamiento(dia, [e.C2014], equipos: [a, b])),
                HttpStatusCode.Created);
            ids.Add(creado.GetProperty("entrenamientoId").GetGuid());
        }

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{ids[0]}"), Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo", equipos: [a, b])));

        foreach (var id in ids)
        {
            var detalle = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{id}")));
            Assert.Equal(2, detalle.GetProperty("equipos").GetArrayLength());
        }

        var primero = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{ids[0]}")));
        var segundo = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync(e.Ruta($"/entrenamientos/{ids[1]}")));
        Assert.Equal("Coliseo", primero.GetProperty("lugar").GetString());
        Assert.Equal("Cancha de Minitas", segundo.GetProperty("lugar").GetString());
    }

    [Fact]
    public async Task El_put_de_datos_rechaza_otros_equipos_y_acepta_los_mismos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], equipos: [b])),
            HttpStatusCode.BadRequest,
            "datos_invalidos");
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], equipos: [])),
            HttpStatusCode.BadRequest,
            "datos_invalidos");
        await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Otro", equipos: [a])));
        await Actividades.LeerAsync(
            await e.E1.Cliente.PutAsync(ruta, Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Otro más")));
    }

    [Fact]
    public async Task Responden_los_del_equipo_y_no_los_de_la_categoria_que_son_de_otro_equipo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Manana, [a]);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var deJ3 = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(ruta));
        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira));
        await Actividades.ErrorAsync(
            await e.Jn(5).Cliente.PutAsync($"{ruta}/mi-respuesta", Asistira), HttpStatusCode.NotFound, "no_encontrado");

        Assert.True(deJ3.GetProperty("permisos").GetProperty("responde").GetBoolean());
    }

    [Fact]
    public async Task La_planilla_trae_a_los_jugadores_de_los_equipos_y_no_a_toda_la_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy(), [a]);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        var planilla = await Actividades.LeerAsync(await e.E1.Cliente.GetAsync($"{ruta}/jugadores"));
        var ids = Actividades.Ids(planilla.GetProperty("jugadores"), "usuarioRolId");

        Assert.Equivalent(new[] { e.Jn(3).Id, e.Jn(4).Id, e.Jn(6).Id }, ids);

        // Registrar a uno de la categoría que no es del equipo se rechaza; a uno del equipo, se acepta.
        await Actividades.ErrorAsync(
            await e.E1.Cliente.PutAsync(
                $"{ruta}/asistencia", new { jugadores = new[] { new { usuarioRolId = e.Jn(5).Id, presente = true } } }),
            HttpStatusCode.Conflict,
            "jugador_fuera_de_la_actividad");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/asistencia", new { jugadores = new[] { new { usuarioRolId = e.Jn(3).Id, presente = true } } }));
    }

    [Fact]
    public async Task Un_equipo_sin_jugadores_se_guarda()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var vacio = await _fabrica.Categorias.CrearEquipoAsync(e.C2014, "Vacío");

        var creado = await Actividades.LeerAsync(
            await e.E1.Cliente.PostAsync(e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014], equipos: [vacio])),
            HttpStatusCode.Created);

        Assert.Equal(1, creado.GetProperty("equipos").GetArrayLength());
    }

    private DateOnly Hoy() => _fabrica.Reloj.Hoy;
}
