using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Estadisticas;

/// <summary>
/// Las estadísticas de la ficha se calculan de lo registrado, por la categoría copiada, y las ve
/// solo quien ve la ficha (historia 6, escenarios 6.1, 6.2 y 6.6; RF-026 a RF-030; CE-003, CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EstadisticasJugadorPruebas
{
    private readonly FabricaApi _fabrica;

    public EstadisticasJugadorPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private static void Cifras(JsonElement cifras, int jugados, int goles, int registrados, int asistidos, int porcentaje)
    {
        Assert.Equal(jugados, cifras.GetProperty("partidosJugados").GetInt32());
        Assert.Equal(goles, cifras.GetProperty("goles").GetInt32());
        Assert.Equal(registrados, cifras.GetProperty("entrenamientosRegistrados").GetInt32());
        Assert.Equal(asistidos, cifras.GetProperty("entrenamientosAsistidos").GetInt32());
        Assert.Equal(porcentaje, cifras.GetProperty("porcentajeAsistencia").GetInt32());
    }

    [Fact]
    public async Task Las_cifras_coinciden_con_lo_registrado_y_cambian_al_corregir()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3).Integrante;
        var jugo = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Hoy.AddDays(-7), convocados: [j3]);
        await _fabrica.Actividades.RegistrarJugadoAsync(jugo, 3, 1, (j3, 2));
        var noJugo = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Hoy.AddDays(-5), convocados: [j3]);
        await _fabrica.Actividades.RegistrarJugadoAsync(noJugo, 1, 1);
        var proximo = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Hoy.AddDays(3), convocados: [j3]);
        await _fabrica.ConContextoAsync(contexto => contexto.PartidosJugador.IgnoreQueryFilters()
            .Where(fila => fila.PartidoId == proximo.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Jugo, true).SetProperty(fila => fila.Goles, 4)));
        foreach (var (dias, presente) in new[] { (-6, true), (-4, true), (-2, false) })
        {
            await _fabrica.Actividades.RegistrarAsistenciaAsync(
                await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(dias)), j3, presente);
        }

        await _fabrica.Actividades.RegistrarAsistenciaAsync(
            await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy.AddDays(-1), estado: EstadoEntrenamiento.CANCELADO), j3, true);
        var ruta = e.Ruta($"/jugadores/{j3.Id}/estadisticas");

        var estadisticas = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(ruta));
        Cifras(estadisticas.GetProperty("total"), 1, 2, 3, 2, 67);
        var de2014 = Assert.Single(estadisticas.GetProperty("porCategoria").EnumerateArray());
        Assert.Equal(2014, de2014.GetProperty("categoria").GetProperty("anio").GetInt32());

        // RF-027: corregir el resultado cambia las cifras en la siguiente consulta.
        await _fabrica.ConContextoAsync(contexto => contexto.PartidosJugador.IgnoreQueryFilters()
            .Where(fila => fila.PartidoId == noJugo.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Jugo, true).SetProperty(fila => fila.Goles, 1)));
        Cifras((await Actividades.LeerAsync(await e.P.Cliente.GetAsync(ruta))).GetProperty("total"), 2, 3, 3, 2, 67);
    }

    [Fact]
    public async Task Quien_cambio_de_categoria_tiene_cifras_en_cada_una()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3).Integrante;
        await _fabrica.Actividades.RegistrarAsistenciaAsync(await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy), j3, true);
        await _fabrica.Actividades.CambiarCategoriaAsync(j3, e.C2013);
        await _fabrica.Actividades.RegistrarAsistenciaAsync(await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2013], Hoy), j3, false);

        var estadisticas = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta($"/jugadores/{j3.Id}/estadisticas")));

        var anios = estadisticas.GetProperty("porCategoria").EnumerateArray()
            .Select(fila => fila.GetProperty("categoria").GetProperty("anio").GetInt32()).ToList();
        Assert.Equal([2014, 2013], anios);
        Cifras(estadisticas.GetProperty("total"), 0, 0, 2, 1, 50);
    }

    [Fact]
    public async Task Sin_registros_todo_esta_en_cero()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var estadisticas = await Actividades.LeerAsync(await e.Jn(5).Cliente.GetAsync(e.Ruta($"/jugadores/{e.Jn(5).Id}/estadisticas")));

        Cifras(estadisticas.GetProperty("total"), 0, 0, 0, 0, 0);
        Assert.Equal(0, estadisticas.GetProperty("porCategoria").GetArrayLength());
    }

    [Fact]
    public async Task Solo_las_ve_quien_ve_la_ficha()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var ruta = e.Ruta($"/jugadores/{e.Jn(3).Id}/estadisticas");
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        Assert.Equal(HttpStatusCode.OK, (await e.P.Cliente.GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.E1.Cliente.GetAsync(ruta)).StatusCode);
        foreach (var cliente in new[] { e.Jn(5).Cliente, e.PB.Cliente, desarrollador })
        {
            await Actividades.ErrorAsync(await cliente.GetAsync(ruta), HttpStatusCode.NotFound, "no_encontrado");
        }
    }
}
