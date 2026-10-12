using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Un PRESIDENTE, DIRECTIVO o ENTRENADOR ve sus datos en "Mi perfil" y cambia nombres, apellidos,
/// fecha de nacimiento y celular en todos sus clubes, sin tocar documento ni correo, sus
/// integrantes JUGADOR ni los nombres copiados (spec 008, historia 4; RF-015 a RF-019; CE-006).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DatosPropiosPruebas
{
    public const string Ruta = "/api/cuenta/datos-propios";

    private readonly FabricaApi _fabrica;

    public DatosPropiosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task E1_D_y_P_leen_sus_siete_datos()
    {
        var c = await _fabrica.Personas.CrearAsync();

        foreach (var persona in new[] { c.E1, c.D, c.P })
        {
            var datos = await LeerAsync(await ClienteAsync(persona.Usuario), Ruta);

            Assert.Equal(persona.Integrante.Nombres, datos.GetProperty("nombres").GetString());
            Assert.Equal(persona.Integrante.Apellidos, datos.GetProperty("apellidos").GetString());
            Assert.Equal("CEDULA_CIUDADANIA", datos.GetProperty("tipoDocumento").GetString());
            Assert.Equal(persona.Integrante.NumeroDocumento, datos.GetProperty("numeroDocumento").GetString());
            Assert.Equal("1985-03-14", datos.GetProperty("fechaNacimiento").GetString());
            Assert.Equal(persona.Usuario.Correo, datos.GetProperty("correo").GetString());
            Assert.Equal(persona.Usuario.Celular, datos.GetProperty("celular").GetString());
        }
    }

    [Fact]
    public async Task Con_roles_en_dos_clubes_salen_los_del_integrante_mas_antiguo()
    {
        var c = await _fabrica.Personas.CrearAsync();

        var datos = await LeerAsync(await ClienteAsync(c.E1.Usuario), Ruta);

        Assert.Equal(c.E1.Integrante.Apellidos, datos.GetProperty("apellidos").GetString());
        Assert.Equal(c.E1.Integrante.NumeroDocumento, datos.GetProperty("numeroDocumento").GetString());
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_404_y_sin_sesion_401()
    {
        var c = await _fabrica.Personas.CrearAsync();

        await NoEncontradoAsync(await (await ClienteAsync(c.Ana.Usuario)).GetAsync(Ruta));
        var sinSesion = await _fabrica.CrearClienteDePrueba().GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
    }

    [Fact]
    public async Task E1_cambia_sus_datos_y_se_ven_en_sus_dos_clubes_y_en_la_sesion()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await ClienteAsync(c.E1.Usuario);

        var guardados = await CuerpoAsync(
            await e1.PutAsync(Ruta, new { nombres = "Ernesto", apellidos = "  Nuevo   Apellido ", fechaNacimiento = "1986-01-02", celular = "3109998877" }));

        Assert.Equal("Nuevo Apellido", guardados.GetProperty("apellidos").GetString());
        Assert.Equal("1986-01-02", guardados.GetProperty("fechaNacimiento").GetString());
        Assert.Equal("3109998877", guardados.GetProperty("celular").GetString());
        Assert.Equal("Nuevo Apellido", (await LeerAsync(e1, Ruta)).GetProperty("apellidos").GetString());
        Assert.Equal("Nuevo Apellido", (await LeerAsync(e1, c.Ficha(c.E1.Integrante))).GetProperty("apellidos").GetString());
        var enB = await LeerAsync(e1, $"/api/clubes/{c.ClubB.Id}/personas/{c.E1EnB.Id}");
        Assert.Equal("Nuevo Apellido", enB.GetProperty("apellidos").GetString());
        var sesion = await LeerAsync(e1, "/api/sesion");
        Assert.All(sesion.GetProperty("clubes").EnumerateArray(), club =>
            Assert.Equal("Nuevo Apellido", club.GetProperty("apellidos").GetString()));
    }

    [Fact]
    public async Task El_celular_cambia_para_la_cuenta_y_sus_jugadores_conservan_sus_nombres()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var (cuenta, _) = await _fabrica.Personas.CrearPersonaAsync(c.Club, Rol.ENTRENADOR, "Mixto", "Dosclubes");
        var jugador = await _fabrica.Sembrador.CrearIntegranteAsync(c.ClubB, cuenta, Rol.JUGADOR, nombres: "Hijo");

        var respuesta = await (await ClienteAsync(cuenta)).PutAsync(
            Ruta, new { nombres = "Cambiado", apellidos = "Cambiado", fechaNacimiento = "1980-01-01", celular = "3205556666" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var (enBase, celular) = await LeerDeLaBaseAsync(jugador);
        Assert.Equal("Hijo", enBase.Nombres);
        Assert.Equal("Pérez", enBase.Apellidos);
        Assert.Equal(new DateOnly(1990, 5, 20), enBase.FechaNacimiento);
        Assert.Equal("3205556666", celular);
    }

    [Fact]
    public async Task Documento_y_correo_en_el_cuerpo_se_ignoran()
    {
        var c = await _fabrica.Personas.CrearAsync();

        var respuesta = await (await ClienteAsync(c.E1.Usuario)).PutAsync(Ruta, new
        {
            nombres = "Ernesto", apellidos = "Otro", fechaNacimiento = "1985-03-14", celular = "3001234567",
            tipoDocumento = "CEDULA_EXTRANJERIA", numeroDocumento = "999999999", correo = "otro@lfclub.test",
        });

        var datos = await CuerpoAsync(respuesta);
        Assert.Equal("CEDULA_CIUDADANIA", datos.GetProperty("tipoDocumento").GetString());
        Assert.Equal(c.E1.Integrante.NumeroDocumento, datos.GetProperty("numeroDocumento").GetString());
        Assert.Equal(c.E1.Usuario.Correo, datos.GetProperty("correo").GetString());
        Assert.Equal(c.E1.Integrante.NumeroDocumento, (await LeerDeLaBaseAsync(c.E1.Integrante)).Integrante.NumeroDocumento);
    }

    [Fact]
    public async Task El_ultimo_cambio_por_P_sigue_con_el_nombre_que_tenia()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var p = await ClienteAsync(c.P.Usuario);

        await CuerpoAsync(await p.PutAsync(
            Ruta, new { nombres = "Pedro", apellidos = "Renombrado", fechaNacimiento = "1985-03-14", celular = "3001234567" }));

        var cambio = (await LeerAsync(p, c.Ruta($"/jugadores/{c.Ana.Integrante.Id}/ficha"))).GetProperty("ultimoCambio");
        Assert.Equal($"Pedro {c.P.Integrante.Apellidos}", cambio.GetProperty("autor").GetString());
    }

    /// <summary>El integrante tal como está en la base, con el celular de su cuenta.</summary>
    private Task<(UsuarioRol Integrante, string? Celular)> LeerDeLaBaseAsync(UsuarioRol integrante) =>
        _fabrica.ConContextoAsync(async contexto =>
        {
            var actual = await contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking().Include(fila => fila.Usuario)
                .FirstAsync(fila => fila.Id == integrante.Id);
            return (actual, actual.Usuario!.Celular);
        });

    private static async Task<JsonElement> CuerpoAsync(HttpResponseMessage respuesta)
    {
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, $"{(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        return await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
    }

    private Task<ClienteDePrueba> ClienteAsync(Usuario cuenta) => _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);
}
