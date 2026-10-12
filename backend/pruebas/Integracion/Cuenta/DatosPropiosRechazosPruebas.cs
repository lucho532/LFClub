using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Los datos propios se validan con las reglas del registro: si algo falla, cada campo dice por
/// qué y no cambia nada en ningún club (spec 008, RF-016; quickstart, bloque 4).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class DatosPropiosRechazosPruebas
{
    private readonly FabricaApi _fabrica;

    public DatosPropiosRechazosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    public static TheoryData<string, string, string, string, string> Invalidos => new()
    {
        { "", "Apellido", "1985-03-14", "3001234567", "nombres" },
        { new string('n', 81), "Apellido", "1985-03-14", "3001234567", "nombres" },
        { "Nombre", "", "1985-03-14", "3001234567", "apellidos" },
        { "Nombre", new string('a', 81), "1985-03-14", "3001234567", "apellidos" },
        { "Nombre", "Apellido", DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd"), "3001234567", "fechaNacimiento" },
        { "Nombre", "Apellido", "1985-03-14", "", "celular" },
        { "Nombre", "Apellido", "1985-03-14", new string('3', 21), "celular" },
    };

    [Theory]
    [MemberData(nameof(Invalidos))]
    public async Task Un_dato_invalido_responde_400_con_su_campo_y_no_cambia_nada(
        string nombres, string apellidos, string fecha, string celular, string campo)
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.E1.Usuario);

        var respuesta = await e1.PutAsync(
            DatosPropiosPruebas.Ruta, new { nombres, apellidos, fechaNacimiento = fecha, celular });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        var errores = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("errores");
        Assert.False(string.IsNullOrWhiteSpace(errores.GetProperty(campo)[0].GetString()));
        await SinCambiosAsync(c.E1.Integrante, c.E1.Usuario);
        await SinCambiosAsync(c.E1EnB, c.E1.Usuario);
    }

    [Fact]
    public async Task Varios_campos_invalidos_dicen_cada_uno_su_motivo()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var e1 = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.E1.Usuario);

        var respuesta = await e1.PutAsync(DatosPropiosPruebas.Ruta, new
        {
            nombres = "", apellidos = "Apellido", fechaNacimiento = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd"),
            celular = new string('3', 25),
        });

        var errores = (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("errores");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(errores.TryGetProperty("nombres", out _));
        Assert.True(errores.TryGetProperty("fechaNacimiento", out _));
        Assert.True(errores.TryGetProperty("celular", out _));
    }

    [Fact]
    public async Task La_cuenta_de_un_jugador_recibe_404()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var familia = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.Ana.Usuario);

        await NoEncontradoAsync(await familia.PutAsync(
            DatosPropiosPruebas.Ruta, new { nombres = "X", apellidos = "Y", fechaNacimiento = "2014-07-15", celular = "3001112233" }));
        var ana = await _fabrica.ConContextoAsync(contexto =>
            contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking().FirstAsync(fila => fila.Id == c.Ana.Integrante.Id));
        Assert.Equal("Ana", ana.Nombres);
    }

    private async Task SinCambiosAsync(UsuarioRol integrante, Usuario cuenta)
    {
        var (actual, celular) = await _fabrica.ConContextoAsync(async contexto =>
        {
            var fila = await contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking().Include(i => i.Usuario)
                .FirstAsync(i => i.Id == integrante.Id);
            return (fila, fila.Usuario!.Celular);
        });

        Assert.Equal(integrante.Nombres, actual.Nombres);
        Assert.Equal(integrante.Apellidos, actual.Apellidos);
        Assert.Equal(integrante.FechaNacimiento, actual.FechaNacimiento);
        Assert.Equal(cuenta.Celular, celular);
    }
}
