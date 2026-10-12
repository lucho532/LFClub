using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Carta;

/// <summary>
/// La rareza sale de la asistencia a los entrenamientos no cancelados de la temporada y se
/// recalcula sola; no toca las estadísticas (spec 010, escenarios 3.10 y 3.11; RF-024, RF-026).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RarezaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public RarezaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    // La temporada es el año de hoy: todo se pone dentro de él, sea el día que sea.
    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private async Task<List<Entrenamiento>> EntrenamientosAsync(EscenarioActividades e, int cuantos) =>
        [.. await Task.WhenAll(Enumerable.Range(0, cuantos).Select(dia =>
            _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], new DateOnly(Hoy.Year, 1, 10).AddDays(dia * 7))))];

    private async Task AsistirAsync(IEnumerable<Entrenamiento> entrenamientos, Persona jugador, int presentes)
    {
        var numero = 0;
        foreach (var entrenamiento in entrenamientos)
        {
            await _fabrica.Actividades.RegistrarAsistenciaAsync(entrenamiento, jugador.Integrante, numero++ < presentes);
        }
    }

    private static async Task<string?> RarezaAsync(EscenarioActividades e, Persona jugador) =>
        (await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{jugador.Id}/carta")))).GetProperty("rareza").GetString();

    [Fact]
    public async Task Los_bordes_de_la_rareza()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var veinte = await EntrenamientosAsync(e, 20);
        var diez = veinte.Take(10).ToList();
        await AsistirAsync(diez, e.Jn(4), 5);
        await AsistirAsync(diez, e.Jn(5), 6);
        await AsistirAsync(diez, e.Jn(6), 9);
        await AsistirAsync(veinte, e.Jn(7), 17);

        Assert.Equal("BRONCE", await RarezaAsync(e, e.Jn(3)));
        Assert.Equal("BRONCE", await RarezaAsync(e, e.Jn(4)));
        Assert.Equal("PLATA", await RarezaAsync(e, e.Jn(5)));
        Assert.Equal("ORO", await RarezaAsync(e, e.Jn(6)));
        Assert.Equal("ORO", await RarezaAsync(e, e.Jn(7)));
    }

    [Fact]
    public async Task Ni_un_cancelado_ni_uno_del_anio_pasado_cuentan()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var diez = await EntrenamientosAsync(e, 10);
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014], new DateOnly(Hoy.Year, 1, 5), estado: EstadoEntrenamiento.CANCELADO);
        var delAnioPasado = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], new DateOnly(Hoy.Year - 1, 12, 20));
        await AsistirAsync(diez, e.Jn(5), 6);
        await _fabrica.Actividades.RegistrarAsistenciaAsync(cancelado, e.Jn(5).Integrante, false);
        await _fabrica.Actividades.RegistrarAsistenciaAsync(delAnioPasado, e.Jn(5).Integrante, false);

        Assert.Equal("PLATA", await RarezaAsync(e, e.Jn(5)));
    }

    [Fact]
    public async Task Corregir_una_asistencia_cambia_la_rareza_y_no_las_estadisticas()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var diez = await EntrenamientosAsync(e, 10);
        await AsistirAsync(diez, e.Jn(4), 5);
        await _fabrica.Cartas.PonerCartaAsync(e.Jn(4).Integrante, estadisticas: [70, 65, 80, 72, 60, 88]);
        Assert.Equal("BRONCE", await RarezaAsync(e, e.Jn(4)));

        await _fabrica.ConContextoAsync(contexto => contexto.AsistenciasEntrenamiento.IgnoreQueryFilters()
            .Where(fila => fila.UsuarioRolId == e.Jn(4).Id && fila.EntrenamientoId == diez[9].Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Presente, true)));

        var carta = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{e.Jn(4).Id}/carta")));
        Assert.Equal("PLATA", carta.GetProperty("rareza").GetString());
        Assert.Equal(73, carta.GetProperty("valoracion").GetInt32());
    }
}
