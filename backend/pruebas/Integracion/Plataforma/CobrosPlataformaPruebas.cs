using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>La generación del cobro mensual de la plataforma (spec 017, historia 4).</summary>
[Collection(ColeccionApi.Nombre)]
public class CobrosPlataformaPruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public CobrosPlataformaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 3), new TimeOnly(10, 0));
    }

    public void Dispose()
    {
        _fabrica.Reloj.UsarElReal();
        _fabrica.CobrosPlataforma.QuitarTarifasAsync().GetAwaiter().GetResult();
    }

    private async Task<Club> ClubConIntegrantesAsync(
        DateOnly? inicio, EstadoClub estado = EstadoClub.ACTIVO, int jugadores = 3, int entrenadores = 2, int directivos = 1, int presidentes = 1)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: estado);
        await _fabrica.CobrosPlataforma.FijarInicioAsync(club, inicio);
        foreach (var (rol, cantidad) in new[]
        {
            (Rol.JUGADOR, jugadores), (Rol.ENTRENADOR, entrenadores), (Rol.DIRECTIVO, directivos), (Rol.PRESIDENTE, presidentes),
        })
        {
            for (var i = 0; i < cantidad; i++)
            {
                await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
            }
        }

        return club;
    }

    [Fact]
    public async Task Sin_precios_definidos_no_se_crea_ningun_cobro()
    {
        var club = await ClubConIntegrantesAsync(Noviembre);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        Assert.Empty(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
    }

    [Fact]
    public async Task Con_precios_y_mes_de_inicio_crea_un_cobro_con_el_conteo_los_precios_y_el_total()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000, entrenador: 5000, directivo: 3000, presidente: 10000, dias: 5);
        var club = await ClubConIntegrantesAsync(Noviembre);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        var cobro = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(Noviembre, cobro.Periodo);
        Assert.Equal(EstadoCobroPlataforma.PENDIENTE, cobro.Estado);
        Assert.Equal((3, 2, 1, 1), (cobro.Jugadores, cobro.Entrenadores, cobro.Directivos, cobro.Presidentes));
        Assert.Equal((1000m, 5000m, 3000m, 10000m), (cobro.PrecioJugador, cobro.PrecioEntrenador, cobro.PrecioDirectivo, cobro.PrecioPresidente));
        Assert.Equal((3 * 1000m) + (2 * 5000m) + 3000m + 10000m, cobro.Total);
        Assert.Equal(5, cobro.DiasDeGracia);
        Assert.Equal(new DateOnly(2026, 11, 8), cobro.SuspendeDesde);
    }

    [Fact]
    public async Task Los_integrantes_en_espera_o_retirados_no_se_cuentan()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var club = await ClubConIntegrantesAsync(Noviembre, jugadores: 2, entrenadores: 0, directivos: 0, presidentes: 0);
        await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        var cobro = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(2, cobro.Jugadores);
        Assert.Equal(2000m, cobro.Total);
    }

    [Fact]
    public async Task Repetir_la_generacion_no_duplica_el_cobro()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var club = await ClubConIntegrantesAsync(Noviembre);

        await _fabrica.CobrosPlataforma.GenerarAsync();
        var segunda = await _fabrica.CobrosPlataforma.GenerarAsync();

        Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(0, segunda);
    }

    [Fact]
    public async Task Dos_generaciones_a_la_vez_crean_un_solo_cobro()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var club = await ClubConIntegrantesAsync(Noviembre);

        await Task.WhenAll(_fabrica.CobrosPlataforma.GenerarAsync(), _fabrica.CobrosPlataforma.GenerarAsync());

        Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
    }

    [Fact]
    public async Task Cambiar_precios_e_integrantes_despues_no_cambia_el_cobro_generado()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000, dias: 5);
        var club = await ClubConIntegrantesAsync(Noviembre);
        await _fabrica.CobrosPlataforma.GenerarAsync();
        var antes = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));

        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 9999, entrenador: 9999, dias: 20);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.CobrosPlataforma.GenerarAsync();

        var despues = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal((antes.Total, antes.Jugadores, antes.PrecioJugador, antes.DiasDeGracia, antes.SuspendeDesde),
            (despues.Total, despues.Jugadores, despues.PrecioJugador, despues.DiasDeGracia, despues.SuspendeDesde));
    }

    [Fact]
    public async Task Un_club_suspendido_tambien_recibe_cobro_y_uno_dado_de_baja_no()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var suspendido = await ClubConIntegrantesAsync(Noviembre, EstadoClub.SUSPENDIDO);
        var dadoDeBaja = await ClubConIntegrantesAsync(Noviembre, EstadoClub.DADO_DE_BAJA);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(suspendido));
        Assert.Empty(await _fabrica.CobrosPlataforma.CobrosDeAsync(dadoDeBaja));
    }

    [Fact]
    public async Task Si_el_total_es_cero_no_se_genera_cobro()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 0, presidente: 0);
        var club = await ClubConIntegrantesAsync(Noviembre);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        Assert.Empty(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
    }

    [Fact]
    public async Task Un_mes_de_inicio_futuro_o_vacio_no_genera_cobro()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var futuro = await ClubConIntegrantesAsync(new DateOnly(2026, 12, 1));
        var vacio = await ClubConIntegrantesAsync(null);

        await _fabrica.CobrosPlataforma.GenerarAsync();

        Assert.Empty(await _fabrica.CobrosPlataforma.CobrosDeAsync(futuro));
        Assert.Empty(await _fabrica.CobrosPlataforma.CobrosDeAsync(vacio));
    }

    [Fact]
    public async Task Un_mes_de_inicio_pasado_genera_solo_el_mes_en_curso()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000);
        var club = await ClubConIntegrantesAsync(new DateOnly(2026, 6, 1));

        await _fabrica.CobrosPlataforma.GenerarAsync();

        var cobro = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(Noviembre, cobro.Periodo);
    }

    [Fact]
    public async Task Si_el_servidor_estuvo_caido_el_plazo_cuenta_desde_el_dia_en_que_se_genera()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000, dias: 5);
        var club = await ClubConIntegrantesAsync(Noviembre);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 3), new TimeOnly(0, 10));

        await _fabrica.CobrosPlataforma.GenerarAsync();

        var cobro = Assert.Single(await _fabrica.CobrosPlataforma.CobrosDeAsync(club));
        Assert.Equal(new DateOnly(2026, 11, 8), cobro.SuspendeDesde);
    }

    [Fact]
    public async Task Tras_diciembre_el_primero_de_enero_genera_el_cobro_de_enero()
    {
        await _fabrica.CobrosPlataforma.FijarTarifasAsync(jugador: 1000, dias: 5);
        var club = await ClubConIntegrantesAsync(new DateOnly(2026, 12, 1));

        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 12, 31), new TimeOnly(23, 50));
        await _fabrica.CobrosPlataforma.GenerarAsync();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2027, 1, 1), new TimeOnly(0, 10));
        await _fabrica.CobrosPlataforma.GenerarAsync();

        var cobros = await _fabrica.CobrosPlataforma.CobrosDeAsync(club);
        Assert.Equal([new DateOnly(2027, 1, 1), new DateOnly(2026, 12, 1)], cobros.Select(c => c.Periodo).ToList());
        Assert.Equal(new DateOnly(2027, 1, 6), cobros[0].SuspendeDesde);
    }

    [Fact]
    public async Task Un_club_creado_queda_con_el_mes_de_inicio_del_cobro_en_el_mes_siguiente()
    {
        // La sesión se abre con el reloj real: las sesiones se emiten con el reloj de la fábrica.
        _fabrica.Reloj.UsarElReal();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 3), new TimeOnly(10, 0));

        var respuesta = await cliente.PostAsync(
            "/api/plataforma/clubes", new { nombre = $"Club {Sembrador.Unico()}", correoPresidente = Sembrador.CorreoUnico() });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var clubId = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("clubId").GetGuid();
        var guardado = await _fabrica.CobrosPlataforma.ClubAsync(new Club { Id = clubId });
        Assert.Equal(new DateOnly(2026, 12, 1), guardado.MesInicioCobro);
    }
}
