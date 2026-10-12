using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Clubes;

/// <summary>El PRESIDENTE mantiene los datos de su club y de ningún otro (RF-010, RF-024, RF-025).</summary>
[Collection(ColeccionApi.Nombre)]
public class ConfiguracionClubPruebas
{
    private readonly FabricaApi _fabrica;

    public ConfiguracionClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_guarda_y_el_cambio_se_ve_en_su_club()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var nombre = $"Renombrado {Sembrador.Unico()}";

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new
        {
            nombre = $"  {nombre} ", sede = "Cancha de Minitas", direccion = " Calle 1 # 2-3 ",
            correoContacto = "contacto@club.co", telefonoContacto = "3001234567",
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("PRESIDENTE", (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("miRol").GetString());
        var guardado = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync($"/api/clubes/{club.Id}"));
        Assert.Equal(nombre, guardado.GetProperty("nombre").GetString());
        Assert.Equal("Cancha de Minitas", guardado.GetProperty("sede").GetString());
        Assert.Equal("Calle 1 # 2-3", guardado.GetProperty("direccion").GetString());
        Assert.Equal("contacto@club.co", guardado.GetProperty("correoContacto").GetString());
        Assert.Equal("3001234567", guardado.GetProperty("telefonoContacto").GetString());
    }

    [Fact]
    public async Task Los_campos_opcionales_vacios_se_guardan_como_nulos()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        await cliente.PutAsync(Ruta(club.Id), new { nombre = club.Nombre, sede = "Sede", direccion = "Dirección" });

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { nombre = club.Nombre, sede = "  ", direccion = (string?)null });

        var dto = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("sede").ValueKind);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("direccion").ValueKind);
    }

    [Fact]
    public async Task El_presidente_de_otro_club_recibe_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (ajeno, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(ajeno);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { nombre = "Tomado por otro" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        var intacto = await _fabrica.ConContextoAsync(contexto => contexto.Clubes.FindAsync(club.Id).AsTask());
        Assert.Equal(club.Nombre, intacto!.Nombre);
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Los_demas_roles_del_club_reciben_403(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new { nombre = "No debería" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Enviar_colores_escudo_o_estado_en_el_cuerpo_no_los_cambia()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new
        {
            nombre = club.Nombre, colorPrincipal = "#FF0000", colorAcento = "#00FF00",
            identidad = new { colorPrincipal = "#FF0000" }, versionEscudo = 9, estado = "DADO_DE_BAJA",
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("identidad").GetProperty("colorPrincipal").ValueKind);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("identidad").GetProperty("urlEscudo").ValueKind);
        Assert.Equal("ACTIVO", dto.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Un_nombre_que_ya_usa_otro_club_responde_409_y_el_propio_nombre_se_puede_conservar()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var repetido = await cliente.PutAsync(Ruta(club.Id), new { nombre = $" {otroClub.Nombre.ToUpperInvariant()} " });
        var elMismo = await cliente.PutAsync(Ruta(club.Id), new { nombre = club.Nombre.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("nombre_de_club_repetido", await ClienteDePrueba.CodigoAsync(repetido));
        Assert.Equal(HttpStatusCode.OK, elMismo.StatusCode);
    }

    [Fact]
    public async Task Los_datos_no_validos_responden_400_por_campo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.PutAsync(Ruta(club.Id), new
        {
            nombre = "", sede = new string('s', 121), direccion = new string('d', 201),
            correoContacto = "no-es-correo", telefonoContacto = new string('1', 21),
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("errores");
        foreach (var campo in new[] { "nombre", "sede", "direccion", "correoContacto", "telefonoContacto" })
        {
            Assert.True(errores.TryGetProperty(campo, out _), $"Falta el error de {campo}");
        }
    }

    private static string Ruta(Guid clubId) => $"/api/clubes/{clubId}/configuracion";
}
