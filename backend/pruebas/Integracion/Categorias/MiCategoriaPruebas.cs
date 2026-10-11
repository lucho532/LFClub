using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;

namespace LaPecosa.Pruebas.Integracion.Categorias;

/// <summary>
/// Una cuenta de jugador solo ve su categoría, sus equipos y el nombre de sus entrenadores
/// (constitución §7.5, §8 y §20; RF-035; historia 7, escenarios 5 a 8; CE-011).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class MiCategoriaPruebas
{
    private readonly FabricaApi _fabrica;

    public MiCategoriaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Ve_su_categoria_sus_equipos_y_el_nombre_de_sus_entrenadores_y_nada_mas()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var equipoA = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var equipoB = await e.Sembrar.CrearEquipoAsync(categoria, "B");
        var desactivado = await e.Sembrar.CrearEquipoAsync(categoria, "Viejo", activo: false);
        var (usuario, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria);
        var (_, companero) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria, apellidos: "Quintero");
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipoA);
        await e.Sembrar.PonerEnEquipoAsync(companero, equipoB);
        var delEntrenador = await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteEntrenador);
        await e.Sembrar.DirigirEquipoAsync(delEntrenador, equipoA);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegrantePresidente);
        await e.Sembrar.AsignarEntrenadorAsync(categoria, e.IntegranteDirectivo, activa: false);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.GetAsync(e.MiCategoria);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var mia = (await respuesta.JsonAsync()).GetProperty("categoria");
        Assert.Equal(2015, mia.GetProperty("anio").GetInt32());
        Assert.Equal(["A"], mia.Lista("equipos").Select(equipo => equipo.GetString()));

        // Los dos asignados hoy, con el equipo que dirige cada uno; no el que ya fue retirado de ella.
        var entrenadores = mia.Lista("entrenadores");
        Assert.Equal(2, entrenadores.Count);
        Assert.All(entrenadores, entrenador =>
        {
            Assert.Equal(e.IntegranteEntrenador.Nombres, entrenador.GetProperty("nombres").GetString());
            Assert.Equal(e.IntegranteEntrenador.Apellidos, entrenador.GetProperty("apellidos").GetString());
        });
        Assert.Equal(
            new[] { "", "A" }, entrenadores.Select(entrenador => string.Join(",", entrenador.Lista("equipos").Select(n => n.GetString()))).Order());

        // Ni identificadores de otros, ni rol, ni contacto, ni compañeros, ni equipos desactivados
        // (RF-035, §23). Desde la 008 sí lleva el identificador de cada entrenador, para abrir su
        // ficha básica (RF-014 de la 008).
        var json = await respuesta.Content.ReadAsStringAsync();
        var ajenos = new[]
        {
            e.IntegranteDirectivo.Id.ToString(), categoria.Id.ToString(),
            equipoA.Id.ToString(), e.IntegranteEntrenador.NumeroDocumento, companero.Apellidos, desactivado.Nombre,
            "\"rol\"", "PRESIDENTE", "equipoId", "correo", "celular", "documento", "@lapecosa.test", "3001234567",
        };
        Assert.All(ajenos, texto => Assert.DoesNotContain(texto, json, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Sin_categoria_lo_dice_y_con_categoria_sin_entrenadores_ni_equipos_las_listas_van_vacias()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var (usuario, _) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria);
        var conCategoria = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var sin = await (await e.Jugador.GetAsync(e.MiCategoria)).JsonAsync();
        var con = (await (await conCategoria.GetAsync(e.MiCategoria)).JsonAsync()).GetProperty("categoria");

        Assert.Equal(System.Text.Json.JsonValueKind.Null, sin.GetProperty("categoria").ValueKind);
        Assert.Equal(2015, con.GetProperty("anio").GetInt32());
        Assert.Empty(con.Lista("equipos"));
        Assert.Empty(con.Lista("entrenadores"));
    }

    [Fact]
    public async Task Cada_jugador_ve_solo_la_suya()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var una = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var otra = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var (deUna, _) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, una);
        var (deOtra, _) = await e.Sembrar.CrearJugadorAsync(e.Club, 2016, otra);

        foreach (var (usuario, anio) in new[] { (deUna, 2015), (deOtra, 2016) })
        {
            var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
            var mia = await (await cliente.GetAsync(e.MiCategoria)).JsonAsync();
            Assert.Equal(anio, mia.GetProperty("categoria").GetProperty("anio").GetInt32());
        }
    }

    [Fact]
    public async Task Solo_la_cuenta_de_un_jugador_de_ese_club_la_consulta()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (retiradoUsuario, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);
        var delRetirado = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(retiradoUsuario);
        var deOtroClub = await _fabrica.CrearClienteDePrueba()
            .ConSesionDeAsync((await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, Rol.JUGADOR)).Usuario);

        foreach (var cliente in new[] { e.Presidente, e.Directivo, e.Entrenador })
        {
            await AislamientoCategoriasPruebas.ComprobarAsync(
                cliente, HttpStatusCode.Forbidden, "rol_no_autorizado", ("GET", e.MiCategoria, null));
        }

        await AislamientoCategoriasPruebas.ComprobarAsync(
            delRetirado, HttpStatusCode.Forbidden, "integrante_retirado", ("GET", e.MiCategoria, null));
        await AislamientoCategoriasPruebas.ComprobarAsync(
            deOtroClub, HttpStatusCode.NotFound, "no_encontrado", ("GET", e.MiCategoria, null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fabrica.CrearClienteDePrueba().GetAsync(e.MiCategoria)).StatusCode);
    }
}
