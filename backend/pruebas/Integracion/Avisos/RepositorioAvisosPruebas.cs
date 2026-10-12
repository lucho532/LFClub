using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Infraestructura.Datos;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.Extensions.DependencyInjection;

namespace LaPecosa.Pruebas.Integracion.Avisos;

/// <summary>
/// El repositorio de avisos de la cuenta de un jugador, sin pasar por la API: agrupado por aviso,
/// quién cuenta, la limitación de la sesión y el aislamiento entre clubes (spec 012, research §3).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RepositorioAvisosPruebas
{
    private readonly FabricaApi _fabrica;

    public RepositorioAvisosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<T> ConRepositorioAsync<T>(Club club, Func<IRepositorioAvisos, Task<T>> operacion)
    {
        await using var ambito = _fabrica.Services.CreateAsyncScope();
        ((ContextoClub)ambito.ServiceProvider.GetRequiredService<IContextoClub>()).Fijar(club.Id);
        return await operacion(ambito.ServiceProvider.GetRequiredService<IRepositorioAvisos>());
    }

    private Task<AvisoDeActividad> AvisarAsync(
        ClubDeJugadores club, string texto, DateTimeOffset cuando, params UsuarioRol[] destinatarios) =>
        _fabrica.Avisos.GenerarAsync(
            club.Club, HechoDeActividad.ENTRENAMIENTO_PROGRAMADO, TipoDeActividad.ENTRENAMIENTO, Guid.NewGuid(),
            texto, destinatarios, cuando);

    [Fact]
    public async Task Un_aviso_que_llega_por_dos_hermanos_sale_una_sola_vez()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            club.Ana.Integrante, EstadoIngreso.APROBADO, categoria: club.C2014, nombres: "Hugo");
        var inicio = DateTimeOffset.UtcNow;
        await AvisarAsync(club, "Nuevo entrenamiento", inicio, club.Ana.Integrante, hugo);

        var lista = await ConRepositorioAsync(club.Club, r => r.ListarVisiblesAsync(club.Ana.Usuario.Id, null, null, 10));
        var sinLeer = await ConRepositorioAsync(club.Club, r => r.ContarSinLeerAsync(club.Ana.Usuario.Id, null));

        Assert.Single(lista);
        Assert.False(lista[0].Leido);
        Assert.Equal(1, sinLeer);
    }

    [Fact]
    public async Task Un_jugador_retirado_o_en_espera_no_cuenta()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        await AvisarAsync(club, "Para Eva y Luis", DateTimeOffset.UtcNow, club.Eva.Integrante, club.Luis);

        var deEva = await ConRepositorioAsync(club.Club, r => r.ContarSinLeerAsync(club.Eva.Usuario.Id, null));
        var deLuis = await ConRepositorioAsync(club.Club, r => r.ListarVisiblesAsync(club.Luis.UsuarioId, null, null, 10));

        Assert.Equal(0, deEva);
        Assert.Empty(deLuis);
    }

    [Fact]
    public async Task La_limitacion_de_la_sesion_deja_ver_solo_lo_del_jugador_elegido()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            club.Ana.Integrante, EstadoIngreso.APROBADO, categoria: club.C2014, nombres: "Hugo");
        var inicio = DateTimeOffset.UtcNow;
        var deAna = await AvisarAsync(club, "Solo Ana", inicio, club.Ana.Integrante);
        var deHugo = await AvisarAsync(club, "Solo Hugo", inicio.AddSeconds(1), hugo);

        var verHugo = await ConRepositorioAsync(
            club.Club, r => r.ListarVisiblesAsync(club.Ana.Usuario.Id, [hugo.Id], null, 10));
        var contarHugo = await ConRepositorioAsync(
            club.Club, r => r.ContarSinLeerAsync(club.Ana.Usuario.Id, [hugo.Id]));
        var ajeno = await ConRepositorioAsync(
            club.Club, r => r.ObtenerParaLecturaAsync(club.Ana.Usuario.Id, [hugo.Id], deAna.Id));

        Assert.Equal([deHugo.Id], verHugo.Select(fila => fila.Aviso.Id).ToList());
        Assert.Equal(1, contarHugo);
        Assert.Null(ajeno);
    }

    [Fact]
    public async Task Otro_club_no_ve_los_avisos()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var otro = await _fabrica.Sembrador.CrearClubAsync();
        var aviso = await AvisarAsync(club, "Nuevo entrenamiento", DateTimeOffset.UtcNow, club.Ana.Integrante);

        var lista = await ConRepositorioAsync(otro, r => r.ListarVisiblesAsync(club.Ana.Usuario.Id, null, null, 10));
        var contador = await ConRepositorioAsync(otro, r => r.ContarSinLeerAsync(club.Ana.Usuario.Id, null));
        var lectura = await ConRepositorioAsync(otro, r => r.ObtenerParaLecturaAsync(club.Ana.Usuario.Id, null, aviso.Id));

        Assert.Empty(lista);
        Assert.Equal(0, contador);
        Assert.Null(lectura);
    }

    [Fact]
    public async Task Va_del_mas_reciente_al_mas_antiguo_con_cursor()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var inicio = DateTimeOffset.UtcNow;
        var primero = await AvisarAsync(club, "1", inicio, club.Ana.Integrante);
        var segundo = await AvisarAsync(club, "2", inicio.AddSeconds(1), club.Ana.Integrante);
        var tercero = await AvisarAsync(club, "3", inicio.AddSeconds(2), club.Ana.Integrante);

        var pagina = await ConRepositorioAsync(club.Club, r => r.ListarVisiblesAsync(club.Ana.Usuario.Id, null, null, 2));
        var siguiente = await ConRepositorioAsync(
            club.Club, r => r.ListarVisiblesAsync(club.Ana.Usuario.Id, null, segundo.Id, 2));

        Assert.Equal([tercero.Id, segundo.Id], pagina.Select(fila => fila.Aviso.Id).ToList());
        Assert.Equal([primero.Id], siguiente.Select(fila => fila.Aviso.Id).ToList());
    }

    [Fact]
    public async Task Leerlo_lo_marca_para_toda_la_cuenta_sin_mover_la_primera_lectura()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            club.Ana.Integrante, EstadoIngreso.APROBADO, categoria: club.C2014, nombres: "Hugo");
        var aviso = await AvisarAsync(club, "Para los dos", DateTimeOffset.UtcNow, club.Ana.Integrante, hugo);
        var primera = new DateTime(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc);

        await ConRepositorioAsync(club.Club, async r =>
        {
            await r.MarcarLeidoAsync(club.Ana.Usuario.Id, aviso.Id, primera);
            await r.MarcarLeidoAsync(club.Ana.Usuario.Id, aviso.Id, primera.AddHours(1));
            return 0;
        });
        var lectura = await ConRepositorioAsync(
            club.Club, r => r.ObtenerParaLecturaAsync(club.Ana.Usuario.Id, null, aviso.Id));
        var filas = await _fabrica.Avisos.LeerAsync(aviso.Id, club.Ana.Usuario);

        Assert.True(lectura!.Leido);
        Assert.Equal(primera, lectura.LeidoEn);
        Assert.All(filas, fila => Assert.Equal(primera, fila.LeidoEn));
        Assert.Equal(2, filas.Count);
    }

    [Fact]
    public async Task Marcar_todos_deja_la_cuenta_en_cero_y_no_toca_a_otra_cuenta()
    {
        var club = await _fabrica.Jugadores.CrearClubAsync();
        var inicio = DateTimeOffset.UtcNow;
        await AvisarAsync(club, "1", inicio, club.Ana.Integrante, club.Caro.Integrante);
        await AvisarAsync(club, "2", inicio.AddSeconds(1), club.Ana.Integrante, club.Caro.Integrante);

        await ConRepositorioAsync(club.Club, async r =>
        {
            await r.MarcarTodosLeidosAsync(club.Ana.Usuario.Id, null, DateTime.UtcNow);
            return 0;
        });
        var ana = await ConRepositorioAsync(club.Club, r => r.ContarSinLeerAsync(club.Ana.Usuario.Id, null));
        var caro = await ConRepositorioAsync(club.Club, r => r.ContarSinLeerAsync(club.Caro.Usuario.Id, null));

        Assert.Equal(0, ana);
        Assert.Equal(2, caro);
    }
}
