using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Pagos;

/// <summary>
/// La mensualidad nace sola cada mes, sin duplicados y solo para quien la debe (spec 015, historia 1;
/// RF-005 a RF-009; CE-001). Como todas las pruebas comparten la base de datos, cada una mira solo los
/// cargos de su propio club.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class GeneracionDeCargosPruebas : IDisposable
{
    private static readonly DateOnly Noviembre = new(2026, 11, 1);

    private readonly FabricaApi _fabrica;

    public GeneracionDeCargosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 11, 15), new TimeOnly(10, 0));
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    [Fact]
    public async Task Cada_jugador_activo_recibe_un_cargo_con_el_valor_del_club_o_el_propio()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        await _fabrica.Pagos.FijarValorPropioAsync(c.Beto.Integrante, 50000m);

        await _fabrica.Pagos.GenerarAsync();

        var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
        Assert.Equal(2, cargos.Count);
        Assert.All(cargos, cargo => Assert.Equal(TipoCargo.MENSUALIDAD, cargo.Tipo));
        Assert.All(cargos, cargo => Assert.Equal(Noviembre, cargo.Mes));
        Assert.Equal(65000m, cargos.Single(cargo => cargo.UsuarioRolId == c.Ana.Integrante.Id).Valor);
        Assert.Equal(50000m, cargos.Single(cargo => cargo.UsuarioRolId == c.Beto.Integrante.Id).Valor);
    }

    [Fact]
    public async Task Dos_ejecuciones_seguidas_no_duplican_ningun_cargo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        await _fabrica.Pagos.GenerarAsync();
        await _fabrica.Pagos.GenerarAsync();

        Assert.Equal(2, (await _fabrica.Pagos.CargosDelClubAsync(c.Club)).Count);
    }

    [Fact]
    public async Task Dos_instancias_a_la_vez_no_duplican_ningun_cargo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        await Task.WhenAll(_fabrica.Pagos.GenerarAsync(), _fabrica.Pagos.GenerarAsync(), _fabrica.Pagos.GenerarAsync());

        Assert.Equal(2, (await _fabrica.Pagos.CargosDelClubAsync(c.Club)).Count);
    }

    [Fact]
    public async Task Quien_no_es_jugador_activo_no_recibe_cargo()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        var (_, enEspera) = await _fabrica.Categorias.CrearJugadorEnEsperaAsync(c.Club, 2014);
        await _fabrica.Pagos.FijarCobraDesdeAsync(enEspera, Noviembre);
        var retirado = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.Categoria, "Eva");
        await _fabrica.Pagos.FijarCobraDesdeAsync(retirado.Integrante, Noviembre);
        await _fabrica.Categorias.RetirarAsync(retirado.Integrante, c.Presidente.Integrante);
        // El entrenador y el directivo no pagan aunque tuvieran una fecha de cobro.
        await _fabrica.Pagos.FijarCobraDesdeAsync(c.Entrenador.Integrante, Noviembre);
        await _fabrica.Pagos.FijarCobraDesdeAsync(c.Directivo.Integrante, Noviembre);

        await _fabrica.Pagos.GenerarAsync();

        var cobrados = (await _fabrica.Pagos.CargosDelClubAsync(c.Club)).Select(cargo => cargo.UsuarioRolId).ToList();
        Assert.Equal(
            new[] { c.Ana.Integrante.Id, c.Beto.Integrante.Id }.Order(),
            cobrados.Order());
    }

    [Fact]
    public async Task Un_club_sin_valor_no_genera_cargos()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(valor: null);

        await _fabrica.Pagos.GenerarAsync();

        Assert.Empty(await _fabrica.Pagos.CargosDelClubAsync(c.Club));
    }

    [Fact]
    public async Task Un_club_suspendido_genera_igual_sus_cargos()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        await _fabrica.ConContextoAsync(async contexto =>
        {
            var club = await contexto.Clubes.FindAsync(c.Club.Id);
            club!.Estado = EstadoClub.SUSPENDIDO;
            return await contexto.SaveChangesAsync();
        });

        await _fabrica.Pagos.GenerarAsync();

        Assert.Equal(2, (await _fabrica.Pagos.CargosDelClubAsync(c.Club)).Count);
    }

    [Fact]
    public async Task El_cargo_guarda_la_categoria_actual_del_jugador()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();

        await _fabrica.Pagos.GenerarAsync();

        var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
        Assert.All(cargos, cargo => Assert.Equal(c.Categoria.Id, cargo.CategoriaId));
    }

    [Fact]
    public async Task No_hay_cargos_retroactivos_ni_antes_de_la_fecha_desde_la_que_se_cobra()
    {
        var c = await _fabrica.Pagos.CrearClubAsync();
        // Beto empieza a pagar en diciembre: en noviembre no se le cobra.
        await _fabrica.Pagos.FijarCobraDesdeAsync(c.Beto.Integrante, new DateOnly(2026, 12, 1));

        await _fabrica.Pagos.GenerarAsync();

        var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
        Assert.Equal([c.Ana.Integrante.Id], cargos.Select(cargo => cargo.UsuarioRolId));
        // Y nunca meses anteriores al en curso, aunque Ana cobre desde antes.
        Assert.DoesNotContain(cargos, cargo => cargo.Mes < Noviembre);
    }

    [Fact]
    public async Task Al_cambiar_de_mes_nace_el_cargo_nuevo_y_el_viejo_conserva_su_valor()
    {
        var c = await _fabrica.Pagos.CrearClubAsync(65000m);
        await _fabrica.Pagos.GenerarAsync();

        await _fabrica.Pagos.FijarValorAsync(c.Club, 70000m);
        _fabrica.Reloj.FijarEnColombia(new DateOnly(2026, 12, 1), new TimeOnly(0, 5));
        await _fabrica.Pagos.GenerarAsync();

        var cargos = await _fabrica.Pagos.CargosDelClubAsync(c.Club);
        Assert.Equal(4, cargos.Count);
        Assert.All(cargos.Where(cargo => cargo.Mes == Noviembre), cargo => Assert.Equal(65000m, cargo.Valor));
        Assert.All(
            cargos.Where(cargo => cargo.Mes == new DateOnly(2026, 12, 1)), cargo => Assert.Equal(70000m, cargo.Valor));
    }
}
