using System.Text.Json;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LaPecosa.Pruebas.Integracion.Personas;

/// <summary>
/// En los ingresos aprobados, la persona lleva a su ficha de hoy y quien aprobó, a su ficha básica
/// mientras siga en el club; en la sala de espera, el hermano lleva a su ficha si es un jugador
/// aprobado (spec 008, historia 2; RF-010 y RF-011; research §8).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EnlacesDeIngresosPruebas
{
    private readonly FabricaApi _fabrica;

    public EnlacesDeIngresosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_jugador_aprobado_lleva_a_su_ficha_aunque_luego_se_retire()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.MarcarAprobadoAsync(c.Caro.Integrante, c.P.Integrante, Rol.JUGADOR);

        ComprobarEnlace((await AprobadoAsync(c, c.Caro.Integrante)).GetProperty("ficha"), c.Caro.Integrante.Id, "JUGADOR");

        await _fabrica.Categorias.RetirarAsync(c.Caro.Integrante, c.P.Integrante);
        ComprobarEnlace((await AprobadoAsync(c, c.Caro.Integrante)).GetProperty("ficha"), c.Caro.Integrante.Id, "JUGADOR");
    }

    [Fact]
    public async Task Un_entrenador_aprobado_lleva_a_su_ficha_basica()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.MarcarAprobadoAsync(c.E2.Integrante, c.P.Integrante, Rol.ENTRENADOR);

        ComprobarEnlace((await AprobadoAsync(c, c.E2.Integrante)).GetProperty("ficha"), c.E2.Integrante.Id, "BASICA");
    }

    [Fact]
    public async Task Aprobado_por_P_lleva_a_su_ficha_basica_y_sin_enlace_tras_quitarlo_del_club()
    {
        var c = await _fabrica.Personas.CrearAsync();
        await _fabrica.Personas.MarcarAprobadoAsync(c.Caro.Integrante, c.D.Integrante, Rol.JUGADOR);

        var antes = await AprobadoAsync(c, c.Caro.Integrante);
        await _fabrica.Personas.QuitarDelClubAsync(c.D.Integrante);
        var despues = await AprobadoAsync(c, c.Caro.Integrante);

        ComprobarEnlace(antes.GetProperty("aprobadoPorFicha"), c.D.Integrante.Id, "BASICA");
        Assert.Equal($"{c.D.Integrante.Nombres} {c.D.Integrante.Apellidos}", despues.GetProperty("aprobadoPor").GetString());
        Assert.Equal(JsonValueKind.Null, despues.GetProperty("aprobadoPorFicha").ValueKind);
    }

    [Fact]
    public async Task Luis_en_espera_lleva_a_la_ficha_de_Ana_tambien_si_Ana_se_retira()
    {
        var c = await _fabrica.Personas.CrearAsync();

        ComprobarEnlace((await EnEsperaAsync(c, c.Luis)).GetProperty("hermanoDeFicha"), c.Ana.Integrante.Id, "JUGADOR");

        await _fabrica.Categorias.RetirarAsync(c.Ana.Integrante, c.P.Integrante);
        ComprobarEnlace((await EnEsperaAsync(c, c.Luis)).GetProperty("hermanoDeFicha"), c.Ana.Integrante.Id, "JUGADOR");
    }

    [Fact]
    public async Task Si_el_hermano_no_esta_aprobado_o_ya_no_existe_no_hay_enlace()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var (_, enEspera) = await _fabrica.Categorias.CrearJugadorEnEsperaAsync(c.Club, 2014);
        var deUnoEnEspera = await _fabrica.Hermanos.CrearHermanoAsync(enEspera);
        var (_, temporal) = await _fabrica.Categorias.CrearJugadorAsync(c.Club, 2014);
        var deUnoQueYaNoEsta = await _fabrica.Hermanos.CrearHermanoAsync(temporal);
        await _fabrica.Personas.QuitarDelClubAsync(temporal);

        Assert.Equal(JsonValueKind.Null, (await EnEsperaAsync(c, deUnoEnEspera)).GetProperty("hermanoDeFicha").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await EnEsperaAsync(c, deUnoQueYaNoEsta)).GetProperty("hermanoDeFicha").ValueKind);
    }

    private async Task<JsonElement> AprobadoAsync(ClubDePersonas c, UsuarioRol integrante) =>
        (await LeerAsync(await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.P.Usuario), c.Ruta("/ingresos/aprobados")))
            .EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == integrante.Id);

    private async Task<JsonElement> EnEsperaAsync(ClubDePersonas c, UsuarioRol integrante) =>
        (await LeerAsync(await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(c.P.Usuario), c.Ruta("/ingresos/en-espera")))
            .EnumerateArray().Single(fila => fila.GetProperty("usuarioRolId").GetGuid() == integrante.Id);
}
