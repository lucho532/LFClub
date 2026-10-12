using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// Un PRESIDENTE o un DIRECTIVO asignado como entrenador conserva su único rol y su alcance: la
/// asignación ni le da ni le quita nada (constitución §8 y §20; RF-017a; historia 3, escenarios 9
/// y 10; CE-012).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EntrenadoresConRolPruebas
{
    private readonly FabricaApi _fabrica;

    public EntrenadoresConRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_presidente_que_se_asigna_a_si_mismo_sigue_siendo_presidente_y_haciendo_lo_de_su_rol()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2015);
        var (_, enEspera) = await e.Sembrar.CrearJugadorEnEsperaAsync(e.Club, 2015);

        var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(categoriaId, e.IntegrantePresidente.Id), new { });

        var entrenador = Assert.Single((await respuesta.JsonAsync()).Lista("entrenadores"));
        Assert.Equal("PRESIDENTE", entrenador.GetProperty("rol").GetString());
        Assert.Equal("PRESIDENTE", await RolEnLaSesionAsync(e.Presidente, e));
        Assert.Equal([Rol.PRESIDENTE], await RolesEnElClubAsync(e.IntegrantePresidente.UsuarioId, e));

        // Sigue pudiendo todo lo de su rol: crear categorías y aprobar ingresos.
        Assert.Equal(HttpStatusCode.Created, (await e.Presidente.PostAsync(e.Categorias, new { anio = 2016 })).StatusCode);
        var aprobacion = await e.Presidente.PostAsync(
            $"/api/clubes/{e.Club.Id}/ingresos/{enEspera.Id}/aprobacion", new { rol = "JUGADOR" });
        Assert.Equal(HttpStatusCode.OK, aprobacion.StatusCode);
    }

    [Fact]
    public async Task Un_directivo_asignado_sigue_siendo_directivo_lo_ve_todo_y_no_modifica_nada()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var suya = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var ajena = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        var inactiva = await e.Sembrar.CrearCategoriaAsync(e.Club, 2017, activa: false);

        var respuesta = await e.Presidente.PutAsync(e.EntrenadorDe(suya.Id, e.IntegranteDirectivo.Id), new { });

        var entrenador = Assert.Single((await respuesta.JsonAsync()).Lista("entrenadores"));
        Assert.Equal("DIRECTIVO", entrenador.GetProperty("rol").GetString());
        Assert.Equal("DIRECTIVO", await RolEnLaSesionAsync(e.Directivo, e));
        Assert.Equal([Rol.DIRECTIVO], await RolesEnElClubAsync(e.IntegranteDirectivo.UsuarioId, e));

        // Su alcance es el de su rol, no el de un entrenador: ve todas, también la ajena y la inactiva.
        Assert.Equal(
            new[] { suya.Id, ajena.Id, inactiva.Id }.Order(),
            (await e.Directivo.ListaAsync(e.Categorias)).Ids("categoriaId").Order());
        Assert.Equal(HttpStatusCode.OK, (await e.Directivo.GetAsync(e.RutaCategoria(ajena.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.Directivo.GetAsync(e.SinCategoria)).StatusCode);

        // Y sigue sin poder modificar nada, ni siquiera en la categoría que entrena.
        await AislamientoCategoriasPruebas.ComprobarAsync(e.Directivo, HttpStatusCode.Forbidden, "rol_no_autorizado",
            ("POST", e.Categorias, "{\"anio\":2018}"),
            ("POST", e.Desactivacion(suya.Id), null),
            ("PUT", e.EntrenadorDe(suya.Id, e.IntegranteEntrenador.Id), null),
            ("DELETE", e.EntrenadorDe(suya.Id, e.IntegranteDirectivo.Id), null));
        Assert.Single((await e.DetalleAsync(suya.Id)).Lista("entrenadores"));
    }

    private static async Task<string?> RolEnLaSesionAsync(ClienteDePrueba cliente, EscenarioCategorias e)
    {
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        return sesion.GetProperty("clubes").EnumerateArray()
            .Single(club => club.GetProperty("clubId").GetGuid() == e.Club.Id)
            .GetProperty("rol").GetString();
    }

    /// <summary>Un integrante tiene un único rol en el club: una sola fila, antes y después.</summary>
    private Task<List<Rol>> RolesEnElClubAsync(Guid usuarioId, EscenarioCategorias e) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(integrante => integrante.UsuarioId == usuarioId && integrante.ClubId == e.Club.Id)
            .Select(integrante => integrante.Rol)
            .ToListAsync());
}
