using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Entrenamientos;

/// <summary>
/// "Próximos entrenamientos" del inicio: hasta cinco que no han terminado, por fecha y hora,
/// cancelados incluidos y marcados (historia 1, escenario 1.9; RF-004, RF-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ProximosEntrenamientosPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public ProximosEntrenamientosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    [Fact]
    public async Task Salen_por_fecha_y_hora_y_como_mucho_cinco()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hoy = _fabrica.Reloj.Hoy;
        var creados = new List<Guid>();
        foreach (var (dias, hora) in new[] { (3, 8), (1, 18), (2, 7), (1, 9), (5, 10), (4, 6), (6, 6) })
        {
            var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync(
                [e.C2014], hoy.AddDays(dias), new TimeOnly(hora, 0), new TimeOnly(hora + 1, 0));
            creados.Add(entrenamiento.Id);
        }

        var proximos = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));

        // Orden: día 1 a las 9, día 1 a las 18, día 2, día 3 y día 4; los de los días 5 y 6 sobran.
        Assert.Equal([creados[3], creados[1], creados[2], creados[0], creados[5]], Actividades.Ids(proximos, "entrenamientoId"));
    }

    [Fact]
    public async Task El_que_termino_no_sale_el_empezado_si_y_el_cancelado_sale_marcado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hoy = _fabrica.Reloj.Hoy;
        var terminado = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], hoy, new TimeOnly(8, 0), new TimeOnly(9, 0));
        var empezado = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], hoy, new TimeOnly(10, 0), new TimeOnly(12, 0));
        var cancelado = await _fabrica.Actividades.CrearEntrenamientoAsync(
            [e.C2014], hoy.AddDays(1), estado: EstadoEntrenamiento.CANCELADO);

        _fabrica.Reloj.FijarEnColombia(hoy, new TimeOnly(11, 0));
        var proximos = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));

        var ids = Actividades.Ids(proximos, "entrenamientoId");
        Assert.DoesNotContain(terminado.Id, ids);
        Assert.Equal([empezado.Id, cancelado.Id], ids);
        Assert.Equal("CANCELADO", proximos[1].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Un_club_sin_entrenamientos_responde_una_lista_vacia()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();

        var proximos = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta("/entrenamientos/proximos")));

        Assert.Equal(0, proximos.GetArrayLength());
    }
}
