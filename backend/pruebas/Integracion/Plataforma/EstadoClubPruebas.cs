using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Transiciones de estado de un club (constitución §7.4 y §20, RF-026 a RF-031).</summary>
[Collection(ColeccionApi.Nombre)]
public class EstadoClubPruebas
{
    private readonly FabricaApi _fabrica;

    public EstadoClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.ACTIVO)]
    public async Task Cada_transicion_permitida_cambia_el_estado_y_registra_quien_y_cuando(EstadoClub desde, EstadoClub hacia)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: desde);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { estado = hacia.ToString() });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(hacia.ToString(), detalle.GetProperty("estado").GetString());
        Assert.True(detalle.GetProperty("estadoCambiadoEn").GetDateTime() >= antes);

        var guardado = await _fabrica.ConContextoAsync(contexto => contexto.Clubes.AsNoTracking().SingleAsync(c => c.Id == club.Id));
        var desarrollador = await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.SingleAsync(u => u.EsDesarrollador));
        Assert.Equal(hacia, guardado.Estado);
        Assert.Equal(desarrollador.Id, guardado.EstadoCambiadoPorUsuarioId);
        Assert.NotNull(guardado.EstadoCambiadoEn);
    }

    [Theory]
    [InlineData(EstadoClub.ACTIVO, EstadoClub.ACTIVO)]
    [InlineData(EstadoClub.SUSPENDIDO, EstadoClub.SUSPENDIDO)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.DADO_DE_BAJA)]
    [InlineData(EstadoClub.DADO_DE_BAJA, EstadoClub.SUSPENDIDO)]
    public async Task Las_transiciones_no_permitidas_responden_409_y_no_cambian_nada(EstadoClub desde, EstadoClub hacia)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: desde);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { estado = hacia.ToString() });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("transicion_no_permitida", await ClienteDePrueba.CodigoAsync(respuesta));
        var guardado = await _fabrica.ConContextoAsync(contexto => contexto.Clubes.AsNoTracking().SingleAsync(c => c.Id == club.Id));
        Assert.Equal(desde, guardado.Estado);
        Assert.Null(guardado.EstadoCambiadoEn);
    }

    [Fact]
    public async Task Tras_suspender_y_levantar_el_club_queda_identico()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/colores", new { colorPrincipal = "#B8370F", colorAcento = "#FFC72C" });
        await cliente.PutAsync($"/api/plataforma/clubes/{club.Id}/configuracion", new { nombre = club.Nombre, sede = "Sede" });
        var antes = await FotoDelClubAsync(cliente, club.Id);

        await cliente.PutAsync(Ruta(club.Id), new { estado = "SUSPENDIDO" });
        await cliente.PutAsync(Ruta(club.Id), new { estado = "ACTIVO" });

        Assert.Equal(antes, await FotoDelClubAsync(cliente, club.Id));
    }

    [Fact]
    public async Task Un_estado_que_no_existe_o_que_falta_responde_400_y_un_club_inexistente_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var inventado = await cliente.PutAsync(Ruta(club.Id), new { estado = "ELIMINADO" });
        var vacio = await cliente.PutAsync(Ruta(club.Id), new { });
        var inexistente = await cliente.PutAsync(Ruta(Guid.NewGuid()), new { estado = "SUSPENDIDO" });

        Assert.Equal(HttpStatusCode.BadRequest, inventado.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(inventado));
        Assert.Equal(HttpStatusCode.BadRequest, vacio.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact]
    public async Task Solo_el_desarrollador_cambia_el_estado()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var conSesion = await cliente.PutAsync(Ruta(club.Id), new { estado = "SUSPENDIDO" });
        var sinSesion = await _fabrica.CrearClienteDePrueba().PutAsync(Ruta(club.Id), new { estado = "SUSPENDIDO" });

        Assert.Equal(HttpStatusCode.Forbidden, conSesion.StatusCode);
        Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(conSesion));
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
    }

    private static string Ruta(Guid clubId) => $"/api/plataforma/clubes/{clubId}/estado";

    /// <summary>Todo lo que el panel ve del club salvo el estado y cuándo cambió, más sus integrantes.</summary>
    private async Task<string> FotoDelClubAsync(ClienteDePrueba cliente, Guid clubId)
    {
        var detalle = await ClienteDePrueba.LeerAsync<Dictionary<string, JsonElement>>(
            await cliente.GetAsync($"/api/plataforma/clubes/{clubId}"));
        detalle.Remove("estadoCambiadoEn");
        var integrantes = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol.IgnoreQueryFilters()
            .Where(i => i.ClubId == clubId).OrderBy(i => i.Id)
            .Select(i => new { i.Id, i.UsuarioId, i.Rol, i.EstadoIngreso, i.NumeroDocumento }).ToListAsync());

        return JsonSerializer.Serialize(new { detalle, integrantes });
    }
}
