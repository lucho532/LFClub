using System.Net;
using System.Text.Json;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>No se puede crear un club sin asignarle un presidente (constitución §20, §12.5).</summary>
[Collection(ColeccionApi.Nombre)]
public class CrearClubPruebas
{
    private readonly FabricaApi _fabrica;

    public CrearClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Crea_el_club_activo_y_neutro_y_envia_la_invitacion_al_presidente()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var nombre = $"Club {Sembrador.Unico()}";
        var correo = Sembrador.CorreoUnico();

        var respuesta = await cliente.PostAsync(
            "/api/plataforma/clubes", new { nombre = $"  {nombre}  ", correoPresidente = $" {correo.ToUpperInvariant()} " });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var club = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(nombre, club.GetProperty("nombre").GetString());
        Assert.Equal("ACTIVO", club.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, club.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
        Assert.Equal(JsonValueKind.Null, club.GetProperty("identidad").GetProperty("urlEscudo").ValueKind);
        Assert.Empty(club.GetProperty("presidentes").EnumerateArray());

        var invitacion = Assert.Single(club.GetProperty("invitaciones").EnumerateArray());
        Assert.Equal(correo, invitacion.GetProperty("correo").GetString());
        Assert.Equal("PRESIDENTE", invitacion.GetProperty("rol").GetString());
        Assert.Equal("ENVIADO", invitacion.GetProperty("estadoEnvio").GetString());
        Assert.False(invitacion.GetProperty("vencida").GetBoolean());
        var dias = invitacion.GetProperty("venceEn").GetDateTime() - invitacion.GetProperty("creadaEn").GetDateTime();
        Assert.Equal(7, dias.TotalDays, 3);

        var enviado = Assert.Single(_fabrica.Correo.Enviados, e => e.Tipo == "invitacion" && e.Destinatario == correo);
        Assert.Equal(nombre, enviado.NombreClub);

        // La respuesta no contiene el token, y la base de datos solo su hash.
        Assert.DoesNotContain(enviado.Token, await respuesta.Content.ReadAsStringAsync());
        Assert.DoesNotContain("token", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var hashes = await _fabrica.ConContextoAsync(contexto =>
            contexto.Invitaciones.IgnoreQueryFilters().Select(i => i.TokenHash).ToListAsync());
        Assert.DoesNotContain(enviado.Token, hashes);

        var lista = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/plataforma/clubes"));
        var enLista = lista.EnumerateArray().Single(c => c.GetProperty("clubId").GetGuid() == club.GetProperty("clubId").GetGuid());
        Assert.False(enLista.GetProperty("presidenteRegistrado").GetBoolean());
    }

    [Theory]
    [InlineData(null, "correoPresidente")]
    [InlineData("", "correoPresidente")]
    [InlineData("no-es-un-correo", "correoPresidente")]
    [InlineData("falta@dominio", "correoPresidente")]
    public async Task Sin_correo_o_con_un_correo_mal_escrito_no_se_crea_nada(string? correo, string campo)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var nombre = $"Club {Sembrador.Unico()}";

        var respuesta = await cliente.PostAsync("/api/plataforma/clubes", new { nombre, correoPresidente = correo });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        var problema = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out _));
        Assert.False(await ExisteClubAsync(nombre));
    }

    [Fact]
    public async Task Sin_nombre_no_se_crea_nada()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync("/api/plataforma/clubes", new { nombre = "  ", correoPresidente = Sembrador.CorreoUnico() });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.True(problema.GetProperty("errores").TryGetProperty("nombre", out _));
    }

    [Fact]
    public async Task Un_nombre_repetido_con_otras_mayusculas_y_espacios_responde_409()
    {
        var existente = await _fabrica.Sembrador.CrearClubAsync($"Atlético {Sembrador.Unico()} F.C.");
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var correo = Sembrador.CorreoUnico();
        var repetido = "  " + existente.Nombre.ToUpperInvariant().Replace(" ", "   ") + " ";

        var respuesta = await cliente.PostAsync("/api/plataforma/clubes", new { nombre = repetido, correoPresidente = correo });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("nombre_de_club_repetido", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
    }

    [Theory]
    [InlineData(FabricaApi.CorreoDesarrollador)]
    [InlineData("  DESARROLLADOR@LFClub.Test ")]
    public async Task El_correo_del_desarrollador_responde_409_y_no_se_crea_nada(string correo)
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var nombre = $"Club {Sembrador.Unico()}";

        var respuesta = await cliente.PostAsync("/api/plataforma/clubes", new { nombre, correoPresidente = correo });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("correo_del_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.False(await ExisteClubAsync(nombre));
    }

    [Fact]
    public async Task Si_el_correo_falla_el_club_se_crea_y_la_invitacion_queda_fallida()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var nombre = $"Club {Sembrador.Unico()}";

        _fabrica.Correo.FallarEnvios = true;
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await cliente.PostAsync("/api/plataforma/clubes", new { nombre, correoPresidente = Sembrador.CorreoUnico() });
        }
        finally
        {
            _fabrica.Correo.FallarEnvios = false;
        }

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var club = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("FALLIDO", club.GetProperty("invitaciones")[0].GetProperty("estadoEnvio").GetString());
        Assert.True(await ExisteClubAsync(nombre));

        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(
            await cliente.GetAsync($"/api/plataforma/clubes/{club.GetProperty("clubId").GetGuid()}"));
        Assert.Equal("FALLIDO", detalle.GetProperty("invitaciones")[0].GetProperty("estadoEnvio").GetString());
    }

    private Task<bool> ExisteClubAsync(string nombre) =>
        _fabrica.ConContextoAsync(contexto => contexto.Clubes.AnyAsync(club => club.Nombre == nombre));
}
