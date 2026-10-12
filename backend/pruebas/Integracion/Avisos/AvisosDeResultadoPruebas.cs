using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Partidos;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// Registrar o publicar el resultado avisa a las familias de todas las categorías del partido,
/// incluidas las no convocadas; la familia que lo publica no recibe su propio aviso y ningún aviso
/// lleva nombres de otros jugadores (spec 012, historia 2; RF-005, RF-014, RF-017 y CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosDeResultadoPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosDeResultadoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private static object Resultado(int club, int rival, params (Persona Jugador, bool Jugo, int Goles)[] filas) =>
        EscenarioResultadoFamilia.DeGestion(club, rival, filas);

    [Fact]
    public async Task El_resultado_del_equipo_avisa_a_toda_la_categoria_incluidas_las_no_convocadas()
    {
        var x = await EscenarioResultadoFamilia.CrearAsync(_fabrica);
        var e = x.E;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{x.RutaX}/resultado", Resultado(3, 1, (e.Jn(3), true, 2), (e.Jn(4), true, 1))));

        // J3 (convocado) y J6 (no convocado, misma categoría) reciben lo mismo; K1 (2015) no.
        foreach (var familia in new[] { e.Jn(3), e.Jn(6) })
        {
            var textos = await AyudaDeAvisos.TextosAsync(e, familia);
            Assert.Equal(["Resultado del partido contra Equipo C: Club de pruebas 3 - 1 Equipo C"], textos.Select(Normalizar));
        }

        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.K1));
    }

    private static string Normalizar(string texto)
    {
        // El nombre del club lo pone el sembrador; el resto del texto es el que fija la redacción.
        var indice = texto.IndexOf(": ", StringComparison.Ordinal);
        var marcador = texto[(indice + 2)..];
        var puntos = marcador.IndexOf(" 3 - 1 ", StringComparison.Ordinal);
        return $"{texto[..(indice + 2)]}Club de pruebas{marcador[puntos..]}";
    }

    [Fact]
    public async Task Corregir_el_marcador_avisa_pero_corregir_solo_un_gol_de_jugador_no()
    {
        var x = await EscenarioResultadoFamilia.CrearAsync(_fabrica);
        var e = x.E;
        var ruta = $"{x.RutaX}/resultado";
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Resultado(3, 1, (e.Jn(3), true, 2), (e.Jn(4), true, 1))));
        var avisos = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, x.X.Id)).Count;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Resultado(3, 1, (e.Jn(3), true, 1), (e.Jn(4), true, 2))));
        var tras = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, x.X.Id)).Count;
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Resultado(2, 1, (e.Jn(3), true, 1), (e.Jn(4), true, 1))));

        Assert.Equal(1, avisos);
        Assert.Equal(1, tras);
        var ultimo = (await AyudaDeAvisos.TextosAsync(e, e.Jn(6))).Single();
        Assert.Contains("2 - 1", ultimo);
    }

    [Fact]
    public async Task El_resultado_de_una_familia_avisa_a_las_demas_y_no_a_quien_lo_publico()
    {
        var x = await EscenarioResultadoFamilia.CrearAsync(_fabrica);
        var e = x.E;

        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync(
            EscenarioResultadoFamilia.MiJugador(x.RutaX), EscenarioResultadoFamilia.Familia(2, 1, true, 1)));

        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
        foreach (var otra in new[] { e.Jn(4), e.Jn(5), e.Jn(6) })
        {
            var textos = await AyudaDeAvisos.TextosAsync(e, otra);
            var texto = Assert.Single(textos);
            Assert.Contains("2 - 1", texto);
            // CE-004: ni el aviso ni su actividad nombran a otros jugadores.
            Assert.DoesNotContain("J3", texto);
            Assert.DoesNotContain("Apellido", texto);
        }

        // Quien indica lo de su jugador después no genera otro aviso: el marcador no cambió.
        var avisos = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, x.X.Id)).Count;
        await Actividades.LeerAsync(await e.Jn(4).Cliente.PutAsync(
            EscenarioResultadoFamilia.MiJugador(x.RutaX), EscenarioResultadoFamilia.Familia(null, null, true, 0)));
        Assert.Equal(avisos, (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, x.X.Id)).Count);
    }
}
