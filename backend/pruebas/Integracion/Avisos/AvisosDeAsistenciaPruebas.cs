using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Avisos;

/// <summary>
/// Registrar la asistencia de un entrenamiento avisa a las categorías en las que algo cambió, con un
/// texto general que no dice quién asistió ni quién faltó (spec 012, historia 1; RF-001, RF-004,
/// RF-016 y CE-004).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosDeAsistenciaPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosDeAsistenciaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Hoy => _fabrica.Reloj.Hoy;

    private static object Marcar(params (Persona Jugador, bool Presente)[] filas) =>
        new { jugadores = filas.Select(fila => new { usuarioRolId = fila.Jugador.Id, presente = fila.Presente }) };

    [Fact]
    public async Task Registrar_avisa_con_un_texto_general_sin_nombres_ni_estados()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}");

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            $"{ruta}/asistencia", Marcar((e.Jn(3), true), (e.Jn(4), false))));

        foreach (var familia in new[] { e.Jn(3), e.Jn(4), e.Jn(5) })
        {
            var aviso = Assert.Single(await AyudaDeAvisos.AvisosAsync(e, familia));
            var texto = aviso.GetProperty("texto").GetString()!;
            Assert.Equal("Se registró la asistencia del entrenamiento de hoy", texto);
            Assert.DoesNotContain("J3", texto);
            Assert.DoesNotContain("J4", texto);
            Assert.DoesNotContain("Apellido", texto);
            Assert.DoesNotContain("presente", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ausente", texto, StringComparison.OrdinalIgnoreCase);
        }

        // La actividad del aviso no trae datos de la asistencia de nadie (RF-017).
        var actividad = (await AyudaDeAvisos.AvisosAsync(e, e.Jn(3))).Single().GetProperty("actividad");
        Assert.Equal(3, actividad.EnumerateObject().Count());
    }

    [Fact]
    public async Task Guardar_la_misma_asistencia_otra_vez_no_avisa_pero_corregirla_si()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}/asistencia");
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Marcar((e.Jn(3), true))));
        var conUno = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id)).Count;

        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Marcar((e.Jn(3), true))));
        var igual = (await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id)).Count;
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(ruta, Marcar((e.Jn(3), false))));

        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        Assert.Equal(1, conUno);
        Assert.Equal(1, igual);
        // El segundo aviso (la corrección) reemplazó al primero sin leer: una sola entrada (RF-025).
        Assert.Single(avisos);
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(5)));
    }

    [Fact]
    public async Task Una_asistencia_nueva_en_una_sola_categoria_de_un_entrenamiento_conjunto_avisa_solo_a_esa()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014, e.C2015], Hoy);
        var ruta = e.Ruta($"/entrenamientos/{entrenamiento.Id}/asistencia");

        await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(ruta, Marcar((e.K1, true))));

        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.K1)); // 2015
        Assert.Empty(await AyudaDeAvisos.TextosAsync(e, e.Jn(3))); // 2014: nada cambió ahí
    }
}
