using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Ficha;

/// <summary>
/// La familia corrige nombres, apellidos y fecha de nacimiento de su propio jugador sin que cambie
/// su categoría (spec 010, historia 2, escenarios 2.1 a 2.7; RF-013 a RF-018; CE-004). Cambia a
/// propósito RF-016 y RF-017 de la 005, que daban 403 a la cuenta del jugador.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CorreccionPorLaFamiliaPruebas
{
    private readonly FabricaApi _fabrica;

    public CorreccionPorLaFamiliaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Cuerpo(UsuarioRol jugador, string? nombres = null, string? apellidos = null, string? fecha = null) => new
    {
        nombres = nombres ?? jugador.Nombres,
        apellidos = apellidos ?? jugador.Apellidos,
        fechaNacimiento = fecha ?? jugador.FechaNacimiento.ToString("yyyy-MM-dd"),
    };

    private Task<UsuarioRol> GuardadoAsync(Persona jugador) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().AsNoTracking().FirstAsync(fila => fila.Id == jugador.Id));

    private static string Identidad(EscenarioActividades e, Persona jugador) => e.Ruta($"/jugadores/{jugador.Id}/ficha/identidad");

    [Fact]
    public async Task La_familia_corrige_apellido_y_fecha_y_sigue_en_su_categoria()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var apellido = $"Corregido {Sembrador.Unico()}";

        var ficha = await Actividades.LeerAsync(
            await j3.Cliente.PutAsync(Identidad(e, j3), Cuerpo(j3.Integrante, apellidos: apellido, fecha: "2013-05-20")));

        Assert.Equal("2013-05-20", ficha.GetProperty("fechaNacimiento").GetString());
        Assert.Equal(2014, ficha.GetProperty("categoriaAnio").GetInt32());
        var cambio = ficha.GetProperty("ultimoCambio");
        Assert.True(cambio.GetProperty("porLaCuentaDelJugador").GetBoolean());
        Assert.True(cambio.TryGetProperty("fecha", out _));
        Assert.Equal(e.C2014.Id, (await GuardadoAsync(j3)).CategoriaId);

        foreach (var persona in new[] { e.P, e.E1 })
        {
            var vista = await Actividades.LeerAsync(await persona.Cliente.GetAsync(e.Ruta($"/jugadores/{j3.Id}/ficha")));
            Assert.Equal(apellido, vista.GetProperty("apellidos").GetString());
        }
    }

    [Fact]
    public async Task Sin_categoria_sigue_sin_categoria_aunque_su_nuevo_anio_tenga_una()
    {
        var e = await EscenarioFicha.CrearAsync(_fabrica);

        var respuesta = await e.CuentaDeCaro.PutAsync(e.Identidad(e.Caro.Id), Cuerpo(e.Caro, fecha: "2015-11-30"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null((await e.JugadorGuardadoAsync(e.Caro.Id)).CategoriaId);
        Assert.Equal(new DateOnly(2015, 11, 30), (await e.JugadorGuardadoAsync(e.Caro.Id)).FechaNacimiento);
    }

    [Fact]
    public async Task Fecha_futura_o_nombres_vacios_responden_400_con_el_motivo()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);
        var manana = _fabrica.Reloj.Hoy.AddDays(2).ToString("yyyy-MM-dd");

        var futura = await j3.Cliente.PutAsync(Identidad(e, j3), Cuerpo(j3.Integrante, fecha: manana));
        var vacios = await j3.Cliente.PutAsync(Identidad(e, j3), Cuerpo(j3.Integrante, nombres: " "));

        Assert.Equal(HttpStatusCode.BadRequest, futura.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, vacios.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(vacios));
    }

    [Fact]
    public async Task Con_dos_hermanos_solo_cambia_el_elegido()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var (j1, j2) = (e.Jn(1), e.Jn(2));

        await Actividades.LeerAsync(await j1.Cliente.PutAsync(Identidad(e, j1), Cuerpo(j1.Integrante, apellidos: "Hermanos Uno")));
        await Actividades.ErrorAsync(
            await j1.Cliente.PutAsync(Identidad(e, j2), Cuerpo(j2.Integrante, apellidos: "Hermanos Dos")), HttpStatusCode.NotFound, "no_encontrado");

        Assert.Equal(j2.Integrante.Apellidos, (await GuardadoAsync(j2)).Apellidos);
        Assert.Equal("Hermanos Uno", (await GuardadoAsync(j1)).Apellidos);
    }

    [Fact]
    public async Task Quien_entrena_y_los_directivos_no_corrigen_y_otra_familia_no_la_encuentra()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var j3 = e.Jn(3);

        foreach (var persona in new[] { e.E1, e.D1 })
        {
            await Actividades.ErrorAsync(
                await persona.Cliente.PutAsync(Identidad(e, j3), Cuerpo(j3.Integrante, apellidos: "Otro")),
                HttpStatusCode.Forbidden,
                "rol_no_autorizado");
        }

        await Actividades.ErrorAsync(
            await e.Jn(4).Cliente.PutAsync(Identidad(e, j3), Cuerpo(j3.Integrante, apellidos: "Otro")), HttpStatusCode.NotFound, "no_encontrado");
    }
}
