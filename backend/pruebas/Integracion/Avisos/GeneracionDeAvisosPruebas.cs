using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Infraestructura.Datos;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.Extensions.DependencyInjection;

namespace LFClub.Pruebas.Integracion.Avisos;

/// <summary>
/// La generación de avisos llamando al servicio directamente, sin pasar por la API: a quién llega,
/// que se excluya a la cuenta que actúa, que cada hermano tenga su fila y que sin destinatarios no
/// se guarde nada (spec 012, RF-005, RF-009, RF-010 y RF-015).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class GeneracionDeAvisosPruebas
{
    private static readonly DateOnly Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

    private readonly FabricaApi _fabrica;

    public GeneracionDeAvisosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task ConServicioAsync(Club club, Func<IServicioAvisosDeActividad, Task> operacion)
    {
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        ((ContextoClub)ambito.ServiceProvider.GetRequiredService<IContextoClub>()).Fijar(club.Id);
        await operacion(ambito.ServiceProvider.GetRequiredService<IServicioAvisosDeActividad>());
    }

    private async Task<List<Guid>> JugadoresQueRecibieronAsync(Entrenamiento entrenamiento)
    {
        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        return avisos.SelectMany(aviso => aviso.Destinatarios).Select(fila => fila.UsuarioRolId).ToList();
    }

    [Fact]
    public async Task Solo_reciben_los_jugadores_activos_de_las_categorias_del_entrenamiento()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([club.C2014], Fecha);

        await ConServicioAsync(club.Club, servicio =>
            servicio.EntrenamientoProgramadoAsync(entrenamiento, club.Presidente.Usuario.Id));

        var recibieron = await JugadoresQueRecibieronAsync(entrenamiento);
        Assert.Equivalent(
            new[] { club.Ana.Integrante.Id, club.Beto.Integrante.Id }, recibieron);
        Assert.DoesNotContain(club.Eva.Integrante.Id, recibieron); // retirada
        Assert.DoesNotContain(club.Luis.Id, recibieron); // en espera
        Assert.DoesNotContain(club.Dani.Integrante.Id, recibieron); // sin categoría
        Assert.DoesNotContain(club.Caro.Integrante.Id, recibieron); // otra categoría
    }

    [Fact]
    public async Task Los_hermanos_de_una_cuenta_tienen_cada_uno_su_fila_del_mismo_aviso()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            club.Ana.Integrante, EstadoIngreso.APROBADO, categoria: club.C2014, nombres: "Hugo");
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([club.C2014], Fecha);

        await ConServicioAsync(club.Club, servicio =>
            servicio.EntrenamientoCanceladoAsync(entrenamiento, club.Presidente.Usuario.Id));

        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        var aviso = Assert.Single(avisos);
        var filasDeLaCuenta = aviso.Destinatarios.Where(fila => fila.UsuarioId == club.Ana.Usuario.Id).ToList();
        Assert.Equivalent(new[] { club.Ana.Integrante.Id, hugo.Id }, filasDeLaCuenta.Select(fila => fila.UsuarioRolId));
        Assert.Equal(HechoDeActividad.ENTRENAMIENTO_CANCELADO, aviso.Hecho);
    }

    [Fact]
    public async Task Excluye_a_la_cuenta_que_hizo_la_accion()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([club.C2014], Fecha);

        await ConServicioAsync(club.Club, servicio =>
            servicio.EntrenamientoReactivadoAsync(entrenamiento, club.Ana.Usuario.Id));

        var recibieron = await JugadoresQueRecibieronAsync(entrenamiento);
        Assert.Equal([club.Beto.Integrante.Id], recibieron);
    }

    [Fact]
    public async Task Sin_destinatarios_no_guarda_nada()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var vacia = await _fabrica.Categorias.CrearCategoriaAsync(club.Club, 2020);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([vacia], Fecha);
        var antes = await _fabrica.Avisos.ContarTodosAsync();

        await ConServicioAsync(club.Club, servicio =>
            servicio.EntrenamientoProgramadoAsync(entrenamiento, club.Presidente.Usuario.Id));

        Assert.Equal(antes, await _fabrica.Avisos.ContarTodosAsync());
        Assert.Empty(await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id));
    }

    [Fact]
    public async Task El_aviso_de_asistencia_solo_llega_a_las_categorias_con_cambio_y_no_lleva_nombres()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([club.C2014, club.C2015], Fecha);

        await ConServicioAsync(club.Club, servicio =>
            servicio.AsistenciaRegistradaAsync(entrenamiento, [club.C2015.Id], club.E1.Usuario.Id));

        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.ENTRENAMIENTO, entrenamiento.Id);
        var aviso = Assert.Single(avisos);
        Assert.Equal([club.Caro.Integrante.Id], aviso.Destinatarios.Select(fila => fila.UsuarioRolId).ToList());
        Assert.DoesNotContain("Caro", aviso.Texto);
        Assert.StartsWith("Se registró la asistencia del entrenamiento", aviso.Texto);
    }

    [Fact]
    public async Task Un_partido_proximo_que_cambia_solo_llega_a_sus_convocados()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [club.C2014], Fecha, convocados: [club.Ana.Integrante]);

        await ConServicioAsync(club.Club, servicio =>
            servicio.PartidoCambiadoAsync(partido, CambiosDeActividad.Lugar, club.E1.Usuario.Id));

        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id);
        var aviso = Assert.Single(avisos);
        Assert.Equal([club.Ana.Integrante.Id], aviso.Destinatarios.Select(fila => fila.UsuarioRolId).ToList());
        Assert.Equal(HechoDeActividad.PARTIDO_CAMBIADO, aviso.Hecho);
    }

    [Fact]
    public async Task Convocar_genera_un_aviso_por_cuenta_que_nombra_a_sus_jugadores_y_solo_ellos_lo_reciben()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            club.Ana.Integrante, EstadoIngreso.APROBADO, categoria: club.C2014, nombres: "Hugo");
        var partido = await _fabrica.Actividades.CrearPartidoAsync([club.C2014], Fecha);

        await ConServicioAsync(club.Club, servicio => servicio.JugadorConvocadoAsync(
            partido, [club.Ana.Integrante.Id, hugo.Id, club.Beto.Integrante.Id], club.E1.Usuario.Id));

        var avisos = await _fabrica.Avisos.DeLaActividadAsync(TipoDeActividad.PARTIDO, partido.Id);
        Assert.Equal(2, avisos.Count);
        var deLaFamilia = avisos.Single(aviso => aviso.Destinatarios.Any(fila => fila.UsuarioId == club.Ana.Usuario.Id));
        Assert.Contains("Ana", deLaFamilia.Texto);
        Assert.Contains("Hugo", deLaFamilia.Texto);
        Assert.Contains("fueron convocados", deLaFamilia.Texto);
        Assert.Equivalent(
            new[] { club.Ana.Integrante.Id, hugo.Id }, deLaFamilia.Destinatarios.Select(fila => fila.UsuarioRolId));
        var deBeto = avisos.Single(aviso => aviso.Destinatarios.Any(fila => fila.UsuarioId == club.Beto.Usuario.Id));
        Assert.Contains("Beto fue convocado", deBeto.Texto);
    }
}
