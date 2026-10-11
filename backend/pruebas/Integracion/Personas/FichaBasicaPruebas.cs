using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using static LaPecosa.Pruebas.Integracion.Personas.LecturaDePersonas;

namespace LaPecosa.Pruebas.Integracion.Personas;

/// <summary>
/// La ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR: su contenido exacto, sin datos
/// privados, y siempre al día con su rol y sus asignaciones (spec 008, historia 1; RF-001, RF-002,
/// RF-004 y RF-006; CE-001).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class FichaBasicaPruebas
{
    private readonly FabricaApi _fabrica;

    public FichaBasicaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_de_E1_dice_entrenador_y_solo_la_2014_con_sus_equipos_activos()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.P.Usuario);

        var ficha = await LeerAsync(cliente, c.Ficha(c.E1.Integrante));

        Assert.Equal(c.E1.Integrante.Id, ficha.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal("Ernesto", ficha.GetProperty("nombres").GetString());
        Assert.Equal(c.E1.Integrante.Apellidos, ficha.GetProperty("apellidos").GetString());
        Assert.Equal("ENTRENADOR", ficha.GetProperty("rol").GetString());
        Assert.True(ficha.GetProperty("versionFoto").GetInt32() > 0);
        Assert.Equal(["2014: A, B"], CategoriasDe(ficha));
    }

    [Fact]
    public async Task La_del_directivo_la_del_presidente_y_la_de_E2()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.E2.Usuario);

        var d = await LeerAsync(cliente, c.Ficha(c.D.Integrante));
        var p = await LeerAsync(cliente, c.Ficha(c.P.Integrante));
        var e2 = await LeerAsync(cliente, c.Ficha(c.E2.Integrante));

        Assert.Equal("DIRECTIVO", d.GetProperty("rol").GetString());
        Assert.Equal(0, d.GetProperty("versionFoto").GetInt32());
        Assert.Equal(["2015: "], CategoriasDe(d));
        Assert.Equal("PRESIDENTE", p.GetProperty("rol").GetString());
        Assert.True(p.GetProperty("versionFoto").GetInt32() > 0);
        Assert.Empty(CategoriasDe(p));
        Assert.Equal("ENTRENADOR", e2.GetProperty("rol").GetString());
        Assert.Empty(CategoriasDe(e2));
    }

    [Fact]
    public async Task No_lleva_documento_fecha_de_nacimiento_correo_ni_celular()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.Ana.Usuario, c.Ana.Integrante.Id);

        foreach (var persona in new[] { c.E1, c.D, c.P })
        {
            var respuesta = await cliente.GetAsync(c.Ficha(persona.Integrante));
            var texto = await respuesta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            foreach (var prohibido in new[] { "documento", "nacimiento", "correo", "celular", "responsable" })
            {
                Assert.DoesNotContain(prohibido, texto, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(persona.Integrante.NumeroDocumento, texto);
            Assert.DoesNotContain(persona.Usuario.Correo, texto);
            Assert.DoesNotContain(persona.Usuario.Celular!, texto);
            Assert.DoesNotContain("1985-03-14", texto);
        }
    }

    [Fact]
    public async Task Al_desactivar_su_asignacion_o_un_equipo_deja_de_salir_en_la_siguiente_consulta()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.P.Usuario);

        await _fabrica.Personas.DesactivarEquipoAsync(c.EquipoB);
        Assert.Equal(["2014: A"], CategoriasDe(await LeerAsync(cliente, c.Ficha(c.E1.Integrante))));

        await _fabrica.Personas.DesactivarAsignacionAsync(c.AsignacionE1);
        Assert.Empty(CategoriasDe(await LeerAsync(cliente, c.Ficha(c.E1.Integrante))));
    }

    [Fact]
    public async Task Al_cambiar_el_rol_del_directivo_la_siguiente_consulta_dice_entrenador()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.P.Usuario);

        await _fabrica.Personas.CambiarRolAsync(c.D.Integrante, Rol.ENTRENADOR);

        Assert.Equal("ENTRENADOR", (await LeerAsync(cliente, c.Ficha(c.D.Integrante))).GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Un_entrenador_en_espera_no_tiene_ficha_basica()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var enEspera = await _fabrica.Personas.CrearPersonaAsync(
            c.Club, Rol.ENTRENADOR, "Esteban", "Espera", estado: EstadoIngreso.EN_ESPERA);
        var cliente = await ClienteAsync(c.P.Usuario);

        await NoEncontradoAsync(await cliente.GetAsync(c.Ficha(enEspera.Integrante)));
    }

    [Fact]
    public async Task Tras_quitar_a_P_del_club_su_ficha_basica_y_su_foto_no_existen()
    {
        var c = await _fabrica.Personas.CrearAsync();
        var cliente = await ClienteAsync(c.D.Usuario);

        await _fabrica.Personas.QuitarDelClubAsync(c.P.Integrante);

        await NoEncontradoAsync(await cliente.GetAsync(c.Ficha(c.P.Integrante)));
        await NoEncontradoAsync(await cliente.GetAsync(c.Foto(c.P.Integrante)));
    }

    private Task<ClienteDePrueba> ClienteAsync(LaPecosa.Dominio.Entidades.Usuario cuenta, Guid? elegido = null) =>
        _fabrica.CrearClienteDePrueba().ElegirJugador(elegido).ConSesionDeAsync(cuenta);
}
