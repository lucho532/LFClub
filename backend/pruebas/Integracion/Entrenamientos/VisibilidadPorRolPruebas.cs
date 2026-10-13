using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// Quién ve un entrenamiento dirigido a equipos: en el calendario, en los próximos, por su
/// identificador y en la campana, y que la regla pura y la consulta dicen lo mismo por rol (spec
/// 013, historia 1, escenarios 1.1 a 1.7; RF-004, RF-018, RF-024; CE-002).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class VisibilidadPorRolPruebas
{
    private readonly FabricaApi _fabrica;

    public VisibilidadPorRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    [Fact]
    public async Task Cada_cuenta_ve_solo_los_entrenamientos_de_su_equipo_y_los_de_toda_la_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(1), [a]);
        var deB = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(2), [b]);
        var deAYB = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(3), [a, b]);
        var deTodos = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(4));

        // J3 solo en A; J5 solo en B; J6 en los dos; J7 sin equipo; J2 y K1 no son de la 2014.
        await ComprobarAsync(e, e.Jn(3), "J3", (deA, true), (deB, false), (deAYB, true), (deTodos, true));
        await ComprobarAsync(e, e.Jn(5), "J5", (deA, false), (deB, true), (deAYB, true), (deTodos, true));
        await ComprobarAsync(e, e.Jn(6), "J6", (deA, true), (deB, true), (deAYB, true), (deTodos, true));
        await ComprobarAsync(e, e.Jn(7), "J7", (deA, false), (deB, false), (deAYB, false), (deTodos, true));
        await ComprobarAsync(e, e.Jn(2), "J2", (deA, false), (deB, false), (deAYB, false), (deTodos, false));
        await ComprobarAsync(e, e.K1, "K1", (deA, false), (deB, false), (deAYB, false), (deTodos, false));

        // Quien entrena la 2014 y el PRESIDENTE los ven todos; el DIRECTIVO sin asignar, solo el de toda la categoría.
        foreach (var (persona, nombre) in new[] { (e.P, "P"), (e.E1, "E1"), (e.E2, "E2"), (e.D1, "D1") })
        {
            await ComprobarAsync(e, persona, nombre, (deA, true), (deB, true), (deAYB, true), (deTodos, true));
        }

        await ComprobarAsync(e, e.D2, "D2", (deA, false), (deB, false), (deAYB, false), (deTodos, true));
        await ComprobarAsync(e, e.M1, "M1", (deA, false), (deB, false), (deAYB, false), (deTodos, false));
    }

    [Fact]
    public async Task El_jugador_en_dos_equipos_ve_el_entrenamiento_una_sola_vez()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deAYB = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(1), [a, b]);

        var proximos = await Actividades.LeerAsync(await e.Jn(6).Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));
        var calendario = await Actividades.LeerAsync(await e.Jn(6).Cliente.GetAsync(
            e.Ruta($"/calendario?desde={Actividades.Texto(Hoy)}&hasta={Actividades.Texto(Hoy.AddDays(6))}")));

        Assert.Single(Actividades.Ids(proximos, "entrenamientoId"), deAYB.Id);
        Assert.Single(Actividades.Ids(calendario.GetProperty("entrenamientos"), "entrenamientoId"), deAYB.Id);
    }

    [Fact]
    public async Task Los_equipos_aparecen_en_el_detalle_y_en_las_listas_para_quien_lo_ve()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(1), [a]);

        var detalle = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(e.Ruta($"/entrenamientos/{deA.Id}")));
        var equipo = Assert.Single(detalle.GetProperty("equipos").EnumerateArray());
        var proximos = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));

        Assert.Equal("A", equipo.GetProperty("nombre").GetString());
        Assert.Equal(2014, equipo.GetProperty("anio").GetInt32());
        Assert.True(equipo.GetProperty("activo").GetBoolean());
        Assert.Equal("A", proximos[0].GetProperty("equipos")[0].GetProperty("nombre").GetString());
        Assert.False(detalle.TryGetProperty("anadidos", out _));
    }

    [Fact]
    public async Task La_regla_pura_y_la_consulta_dicen_lo_mismo_por_rol()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, b) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var anadido = e.Jn(7);
        var entrenamientos = new[]
        {
            await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(1), [a]),
            await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014, e.C2015], Hoy.AddDays(2), [b]),
            await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(3)),
            await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Hoy.AddDays(4)),
        };
        await _fabrica.Actividades.AnadirJugadorAsync(entrenamientos[0], anadido.Integrante);
        await _fabrica.Actividades.RegistrarAsistenciaAsync(entrenamientos[3], e.Jn(4).Integrante, presente: true);

        var entrenan = new Dictionary<Guid, HashSet<Guid>>
        {
            [e.E1.Id] = [e.C2014.Id],
            [e.E2.Id] = [e.C2014.Id, e.C2015.Id],
            [e.D1.Id] = [e.C2014.Id],
        };
        var personas = new List<Persona> { e.P, e.E1, e.E2, e.D1, e.D2, e.K1, e.M1 };
        personas.AddRange(Enumerable.Range(1, 10).Select(e.Jn));

        foreach (var persona in personas)
        {
            var rol = persona.Integrante.Rol;
            var misEquipos = (await _fabrica.ConContextoAsync(contexto => contexto.JugadoresEquipo.IgnoreQueryFilters()
                .Where(fila => fila.UsuarioRolId == persona.Id).Select(fila => fila.EquipoId).ToListAsync())).ToHashSet();
            foreach (var entrenamiento in entrenamientos)
            {
                var filas = await _fabrica.ConContextoAsync(async contexto => new
                {
                    Categorias = await contexto.EntrenamientosCategoria.IgnoreQueryFilters()
                        .Where(fila => fila.EntrenamientoId == entrenamiento.Id).Select(fila => fila.CategoriaId).ToListAsync(),
                    Equipos = await contexto.EntrenamientosEquipo.IgnoreQueryFilters()
                        .Where(fila => fila.EntrenamientoId == entrenamiento.Id).Select(fila => fila.EquipoId).ToListAsync(),
                    EnLista = await contexto.EntrenamientosJugador.IgnoreQueryFilters()
                        .AnyAsync(fila => fila.EntrenamientoId == entrenamiento.Id && fila.UsuarioRolId == persona.Id),
                    Asistencia = await contexto.AsistenciasEntrenamiento.IgnoreQueryFilters()
                        .AnyAsync(fila => fila.EntrenamientoId == entrenamiento.Id && fila.UsuarioRolId == persona.Id),
                });
                var entrena = entrenan.TryGetValue(persona.Id, out var categorias) ? categorias : [];
                var esperado = ReglaVisibilidadDeEntrenamiento.LaVe(
                    rol,
                    filas.Categorias.Any(entrena.Contains),
                    persona.Integrante.CategoriaId is { } suCategoria && filas.Categorias.Contains(suCategoria),
                    filas.Asistencia,
                    filas.Equipos.Count > 0,
                    filas.Equipos.Any(misEquipos.Contains),
                    filas.EnLista);

                var respuesta = await persona.Cliente.GetAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}"));

                Assert.True(
                    esperado == (respuesta.StatusCode == HttpStatusCode.OK),
                    $"{rol} {persona.Integrante.Nombres}: regla {esperado}, API {(int)respuesta.StatusCode}");
            }
        }
    }

    [Fact]
    public async Task El_entrenamiento_de_un_equipo_de_otro_club_no_se_ve_con_la_sesion_de_este_club()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (a, _) = await _fabrica.Actividades.CrearEquiposAsync(e);
        var deA = await _fabrica.Actividades.CrearEntrenamientoDeEquiposAsync([e.C2014], Hoy.AddDays(1), [a]);

        var porElOtroClub = await e.PB.Cliente.GetAsync(e.RutaB($"/entrenamientos/{deA.Id}"));
        var porLaRutaAjena = await e.PB.Cliente.GetAsync(e.Ruta($"/entrenamientos/{deA.Id}"));

        Assert.Equal(HttpStatusCode.NotFound, porElOtroClub.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, porLaRutaAjena.StatusCode);
    }

    private async Task ComprobarAsync(
        EscenarioActividades e, Persona persona, string quien, params (Entrenamiento Entrenamiento, bool LoVe)[] esperados)
    {
        var calendario = await Actividades.LeerAsync(await persona.Cliente.GetAsync(
            e.Ruta($"/calendario?desde={Actividades.Texto(Hoy)}&hasta={Actividades.Texto(Hoy.AddDays(6))}")));
        var proximos = await Actividades.LeerAsync(await persona.Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));
        var enElCalendario = Actividades.Ids(calendario.GetProperty("entrenamientos"), "entrenamientoId");
        var enLosProximos = Actividades.Ids(proximos, "entrenamientoId");

        foreach (var (entrenamiento, loVe) in esperados)
        {
            var detalle = await persona.Cliente.GetAsync(e.Ruta($"/entrenamientos/{entrenamiento.Id}"));

            Assert.True(loVe == enElCalendario.Contains(entrenamiento.Id), $"{quien}: calendario {entrenamiento.Fecha}");
            Assert.True(loVe == enLosProximos.Contains(entrenamiento.Id), $"{quien}: próximos {entrenamiento.Fecha}");
            Assert.True(
                (loVe ? HttpStatusCode.OK : HttpStatusCode.NotFound) == detalle.StatusCode,
                $"{quien}: detalle {entrenamiento.Fecha} {(int)detalle.StatusCode}");
        }
    }
}
