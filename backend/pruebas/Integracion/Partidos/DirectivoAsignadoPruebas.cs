using System.Net;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Partidos;

/// <summary>
/// Un DIRECTIVO asignado a una categoría actúa como su entrenador en lo deportivo, y solo en ella;
/// sin asignación, un partido próximo no existe para él (constitución 4.6.0, §8 y §17.2).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DirectivoAsignadoPruebas
{
    private readonly FabricaApi _fabrica;

    public DirectivoAsignadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private DateOnly EnTresDias => _fabrica.Reloj.Hoy.AddDays(3);

    [Fact]
    public async Task El_directivo_asignado_ve_cambia_y_convoca_en_un_partido_de_su_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], EnTresDias);
        var ruta = e.Ruta($"/partidos/{partido.Id}");

        Assert.Equal(HttpStatusCode.OK, (await e.D1.Cliente.GetAsync(ruta)).StatusCode);
        var cambio = await e.D1.Cliente.PutAsync(ruta, Actividades.Partido(EnTresDias, [e.C2014], rival: "Equipo D"));
        Assert.Equal("Equipo D", (await Actividades.LeerAsync(cambio)).GetProperty("rival").GetString());

        var convocatoria = await e.D1.Cliente.PutAsync($"{ruta}/convocatoria", new { usuarioRolIds = new[] { e.Jn(3).Id } });
        Assert.Equal(1, (await Actividades.LeerAsync(convocatoria)).GetProperty("convocados").GetArrayLength());
    }

    [Fact]
    public async Task Fuera_de_su_categoria_y_sin_asignacion_un_partido_proximo_no_existe()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], EnTresDias);
        var de2016 = await _fabrica.Actividades.CrearPartidoAsync([e.C2016], EnTresDias);

        await Actividades.ErrorAsync(
            await e.D1.Cliente.GetAsync(e.Ruta($"/partidos/{de2016.Id}")), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.D2.Cliente.GetAsync(e.Ruta($"/partidos/{de2014.Id}")), HttpStatusCode.NotFound, "no_encontrado");
    }

    [Fact]
    public async Task Al_perder_la_asignacion_el_partido_desaparece_para_el()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], EnTresDias);
        Assert.Equal(HttpStatusCode.OK, (await e.D1.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}"))).StatusCode);

        await _fabrica.ConContextoAsync(contexto => contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
            .Where(asignacion => asignacion.UsuarioRolId == e.D1.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(asignacion => asignacion.Activa, false)));

        await Actividades.ErrorAsync(
            await e.D1.Cliente.GetAsync(e.Ruta($"/partidos/{partido.Id}")), HttpStatusCode.NotFound, "no_encontrado");
        Assert.Equal(0, (await Actividades.LeerAsync(await e.D1.Cliente.GetAsync(e.Ruta("/partidos")))).GetArrayLength());
    }
}
