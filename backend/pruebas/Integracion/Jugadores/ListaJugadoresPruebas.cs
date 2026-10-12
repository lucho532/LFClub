using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Jugadores;

/// <summary>
/// El PRESIDENTE y los DIRECTIVOS consultan la lista "Jugadores": exactamente los activos,
/// agrupados por categoría y sin ningún dato de la ficha ni de pago (spec 007, historia 1; RF-001 a
/// RF-006; CE-001 y CE-007).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ListaJugadoresPruebas
{
    private static readonly string[] CamposDelJugador = ["usuarioRolId", "nombres", "apellidos", "equipos", "fotoActualizadaEn"];

    private readonly FabricaApi _fabrica;

    public ListaJugadoresPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_y_el_directivo_reciben_exactamente_los_activos_por_categoria()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();

        foreach (var cuenta in new[] { c.Presidente.Usuario, c.Directivo.Usuario })
        {
            var grupos = await ListarAsync(await _fabrica.Jugadores.ClienteAsync(cuenta), c);

            // Por año de menor a mayor y "Sin categoría" al final; sin Eva (retirada) ni Luis (en espera).
            Assert.Equal([2014, 2015, null], grupos.Select(AnioDe).ToList());
            Assert.Equal([c.C2014.Id, c.C2015.Id], grupos.Take(2).Select(g => g.GetProperty("categoria").GetProperty("categoriaId").GetGuid()));
            Assert.Equal([c.Ana.Integrante.Id, c.Beto.Integrante.Id], IdsDe(grupos[0]));
            Assert.Equal([c.Caro.Integrante.Id], IdsDe(grupos[1]));
            Assert.Equal([c.Dani.Integrante.Id], IdsDe(grupos[2]));

            var ana = grupos[0].GetProperty("jugadores")[0];
            Assert.Equal("Ana", ana.GetProperty("nombres").GetString());
            Assert.Equal(c.Ana.Integrante.Apellidos, ana.GetProperty("apellidos").GetString());
            Assert.Equal(["A", "B"], EquiposDe(ana));
            Assert.Empty(EquiposDe(grupos[0].GetProperty("jugadores")[1]));
            Assert.Equal(["C"], EquiposDe(grupos[1].GetProperty("jugadores")[0]));
            Assert.Equal(JsonValueKind.Null, ana.GetProperty("fotoActualizadaEn").ValueKind);
        }
    }

    [Fact]
    public async Task Dentro_de_cada_grupo_los_jugadores_van_por_apellidos_y_nombres()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var (_, primero) = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.C2014, "Zoe", "Aaa");
        var (_, ultimo) = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014, c.C2014, "Abel", "zzz");

        var grupos = await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario), c);

        Assert.Equal([primero.Id, c.Ana.Integrante.Id, c.Beto.Integrante.Id, ultimo.Id], IdsDe(grupos[0]));
    }

    [Fact]
    public async Task Un_equipo_desactivado_no_aparece_en_los_equipos_del_jugador()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.ConContextoAsync(contexto => contexto.Equipos.IgnoreQueryFilters()
            .Where(equipo => equipo.Id == c.EquipoB.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(equipo => equipo.Activo, false)));

        var grupos = await ListarAsync(await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario), c);

        Assert.Equal(["A"], EquiposDe(grupos[0].GetProperty("jugadores")[0]));
    }

    [Fact]
    public async Task Sin_jugadores_sin_categoria_no_hay_ese_grupo_y_un_club_nuevo_da_lista_vacia()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.Jugadores.ClienteAsync(presidente);

        var vacia = await cliente.GetAsync($"/api/clubes/{club.Id}/jugadores");
        Assert.Equal(HttpStatusCode.OK, vacia.StatusCode);
        Assert.Empty(await ClienteDePrueba.LeerAsync<List<JsonElement>>(vacia));

        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        await _fabrica.Categorias.CrearJugadorAsync(club, 2014, categoria);
        var conUno = await ClienteDePrueba.LeerAsync<List<JsonElement>>(await cliente.GetAsync($"/api/clubes/{club.Id}/jugadores"));
        Assert.Equal([2014], conUno.Select(AnioDe).ToList());
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_403_rol_no_autorizado()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        var familia = await _fabrica.Jugadores.ClienteAsync(c.Ana.Usuario, c.Ana.Integrante);

        var respuesta = await familia.GetAsync(c.Ruta("/jugadores"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task La_respuesta_no_lleva_datos_de_la_ficha_ni_de_pago()
    {
        var c = await _fabrica.Jugadores.CrearClubAsync();
        await _fabrica.Fichas.CrearFichaAsync(c.Ana.Integrante);
        var cliente = await _fabrica.Jugadores.ClienteAsync(c.Presidente.Usuario);

        var respuesta = await cliente.GetAsync(c.Ruta("/jugadores"));
        var texto = await respuesta.Content.ReadAsStringAsync();
        var grupos = await ClienteDePrueba.LeerAsync<List<JsonElement>>(respuesta);

        Assert.All(
            grupos.SelectMany(grupo => grupo.GetProperty("jugadores").EnumerateArray()),
            jugador => Assert.Equal(CamposDelJugador, jugador.EnumerateObject().Select(campo => campo.Name)));
        Assert.All(grupos, grupo => Assert.Equal(["categoria", "jugadores"], grupo.EnumerateObject().Select(campo => campo.Name)));
        Assert.DoesNotContain(c.Ana.Integrante.NumeroDocumento, texto);
        Assert.DoesNotContain(c.Ana.Usuario.Correo, texto);
        Assert.DoesNotContain(SembradorFichas.Alergias, texto);
        foreach (var palabra in new[] { "documento", "correo", "telefono", "celular", "salud", "documentacion", "pago", "aldia" })
        {
            Assert.DoesNotContain(palabra, texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static async Task<List<JsonElement>> ListarAsync(ClienteDePrueba cliente, ClubDeJugadores c)
    {
        var respuesta = await cliente.GetAsync(c.Ruta("/jugadores"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await ClienteDePrueba.LeerAsync<List<JsonElement>>(respuesta);
    }

    internal static int? AnioDe(JsonElement grupo) =>
        grupo.GetProperty("categoria") is { ValueKind: JsonValueKind.Object } categoria
            ? categoria.GetProperty("anio").GetInt32()
            : null;

    internal static List<Guid> IdsDe(JsonElement grupo) =>
        grupo.GetProperty("jugadores").EnumerateArray().Select(j => j.GetProperty("usuarioRolId").GetGuid()).ToList();

    private static List<string> EquiposDe(JsonElement jugador) =>
        jugador.GetProperty("equipos").EnumerateArray().Select(e => e.GetString()!).ToList();
}
