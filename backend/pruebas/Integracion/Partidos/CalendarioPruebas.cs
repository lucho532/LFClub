using System.Net;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Partidos;

/// <summary>
/// El calendario: un rango de hasta 62 días, ambos incluidos, con lo que quien pregunta puede ver
/// (research §12).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CalendarioPruebas
{
    private readonly FabricaApi _fabrica;

    public CalendarioPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private static string Rango(DateOnly desde, DateOnly hasta) =>
        $"/calendario?desde={Actividades.Texto(desde)}&hasta={Actividades.Texto(hasta)}";

    [Fact]
    public async Task Un_rango_de_62_dias_incluye_los_dos_extremos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var hasta = Hoy.AddDays(61);
        var primero = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ultimo = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], hasta);
        var fuera = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], hasta.AddDays(1));

        var calendario = await Actividades.LeerAsync(await e.Jn(3).Cliente.GetAsync(e.Ruta(Rango(Hoy, hasta))));

        Assert.Equal([primero.Id, ultimo.Id], Actividades.Ids(calendario.GetProperty("entrenamientos"), "entrenamientoId"));
        Assert.DoesNotContain(fuera.Id, Actividades.Ids(calendario.GetProperty("entrenamientos"), "entrenamientoId"));
        Assert.Equal(Actividades.Texto(Hoy), calendario.GetProperty("desde").GetString());
    }

    [Fact]
    public async Task El_partido_solo_aparece_en_el_calendario_de_sus_convocados_tambien_entre_hermanos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var convocados = new[] { 1, 3, 4, 5, 6, 7, 8 };
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Hoy.AddDays(1), convocados: convocados.Select(numero => e.Jn(numero).Integrante));
        var cancelado = await _fabrica.Actividades.CrearPartidoAsync(
            [e.C2014], Hoy.AddDays(2), convocados: [e.Jn(3).Integrante], estado: LFClub.Dominio.Enumeraciones.EstadoPartido.CANCELADO);

        for (var numero = 1; numero <= 10; numero++)
        {
            var calendario = await Actividades.LeerAsync(await e.Jn(numero).Cliente.GetAsync(e.Ruta(Rango(Hoy, Hoy.AddDays(6)))));
            var ids = Actividades.Ids(calendario.GetProperty("partidos"), "partidoId");
            Assert.True(convocados.Contains(numero) == ids.Contains(partido.Id), $"J{numero}");
            Assert.True((numero == 3) == ids.Contains(cancelado.Id), $"J{numero} cancelado");
        }

        var dePresidente = await Actividades.LeerAsync(await e.P.Cliente.GetAsync(e.Ruta(Rango(Hoy, Hoy.AddDays(6)))));
        Assert.Equal(2, dePresidente.GetProperty("partidos").GetArrayLength());
        var deD2 = await Actividades.LeerAsync(await e.D2.Cliente.GetAsync(e.Ruta(Rango(Hoy, Hoy.AddDays(6)))));
        Assert.Equal(0, deD2.GetProperty("partidos").GetArrayLength());
    }

    [Fact]
    public async Task Un_rango_de_63_dias_al_reves_o_sin_fechas_se_rechaza()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var rutas = new[]
        {
            Rango(Hoy, Hoy.AddDays(62)),
            Rango(Hoy, Hoy.AddDays(-1)),
            "/calendario",
            $"/calendario?desde={Actividades.Texto(Hoy)}&hasta=mañana",
        };

        foreach (var ruta in rutas)
        {
            await Actividades.ErrorAsync(await e.P.Cliente.GetAsync(e.Ruta(ruta)), HttpStatusCode.BadRequest, "datos_invalidos");
        }
    }
}
