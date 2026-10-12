using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Quién ve cada entrenamiento en el calendario, en "Próximos entrenamientos" y por su
/// identificador: las tres respuestas coinciden para cada cuenta (historia 1, escenarios 1.1 y
/// 1.3; RF-003; research §4).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class VisibilidadEntrenamientosPruebas
{
    private readonly FabricaApi _fabrica;

    public VisibilidadEntrenamientosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    [Fact]
    public async Task Cada_cuenta_ve_lo_suyo_en_el_calendario_en_los_proximos_y_por_identificador()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var conjunto = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Hoy.AddDays(1));
        var de2016 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2016], Hoy.AddDays(2));

        var esperado = new Dictionary<string, (Persona Persona, bool Conjunto, bool De2016)>
        {
            ["P"] = (e.P, true, true),
            ["D1"] = (e.D1, true, true),
            ["D2"] = (e.D2, true, true),
            ["E1"] = (e.E1, true, false),
            ["E2"] = (e.E2, true, false),
            ["K1"] = (e.K1, true, false),
            ["M1"] = (e.M1, false, true),
        };
        for (var numero = 1; numero <= 10; numero++)
        {
            esperado[$"J{numero}"] = (e.Jn(numero), true, false);
        }

        foreach (var (nombre, (persona, conjuntoVisible, de2016Visible)) in esperado)
        {
            await ComprobarAsync(e, persona, conjunto, conjuntoVisible, nombre);
            await ComprobarAsync(e, persona, de2016, de2016Visible, nombre);
        }
    }

    [Fact]
    public async Task Un_jugador_que_entra_a_la_categoria_ve_sus_entrenamientos_desde_ese_momento()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(1));
        await ComprobarAsync(e, e.M1, de2014, false, "M1 en la 2016");

        await _fabrica.Actividades.CambiarCategoriaAsync(e.M1.Integrante, e.C2014);

        await ComprobarAsync(e, e.M1, de2014, true, "M1 en la 2014");
    }

    [Fact]
    public async Task Quien_pierde_la_asignacion_deja_de_verlo_al_instante()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(1));
        await ComprobarAsync(e, e.E1, de2014, true, "E1 asignado");

        await _fabrica.ConContextoAsync(contexto => contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
            .Where(asignacion => asignacion.UsuarioRolId == e.E1.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, false)));

        await ComprobarAsync(e, e.E1, de2014, false, "E1 sin asignación");
    }

    [Fact]
    public async Task Los_hermanos_de_una_misma_cuenta_ven_cada_uno_lo_de_su_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(1));
        var de2015 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Hoy.AddDays(1));

        await ComprobarAsync(e, e.Jn(1), de2014, true, "J1");
        await ComprobarAsync(e, e.Jn(1), de2015, false, "J1");
        await ComprobarAsync(e, e.Jn(2), de2014, false, "J2");
        await ComprobarAsync(e, e.Jn(2), de2015, true, "J2");
    }

    /// <summary>El calendario, los próximos y la consulta por identificador dicen lo mismo.</summary>
    private async Task ComprobarAsync(EscenarioActividades e, Persona persona, Entrenamiento entrenamiento, bool loVe, string quien)
    {
        var calendario = await Actividades.LeerAsync(await persona.Cliente.GetAsync(
            e.Ruta($"/calendario?desde={Actividades.Texto(Hoy)}&hasta={Actividades.Texto(Hoy.AddDays(6))}")));
        var proximos = await Actividades.LeerAsync(await persona.Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));
        var detalle = await persona.Cliente.GetAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}"));

        Assert.True(loVe == Actividades.Ids(calendario.GetProperty("entrenamientos"), "entrenamientoId").Contains(entrenamiento.Id), $"{quien}: calendario");
        Assert.True(loVe == Actividades.Ids(proximos, "entrenamientoId").Contains(entrenamiento.Id), $"{quien}: próximos");
        Assert.True((loVe ? HttpStatusCode.OK : HttpStatusCode.NotFound) == detalle.StatusCode, $"{quien}: detalle {(int)detalle.StatusCode}");
    }
}
