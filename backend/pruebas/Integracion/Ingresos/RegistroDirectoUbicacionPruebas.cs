using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Quien entra con una invitación de JUGADOR queda, al registrarse, en la categoría activa de su
/// año de nacimiento, o sin categoría si el club no la tiene; quien entra como ENTRENADOR o
/// DIRECTIVO nunca tiene categoría ni ningún dato como jugador (constitución §12.1 y §20; RF-011 y
/// RF-012; escenarios 2.4 a 2.6; CE-004 y CE-005).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RegistroDirectoUbicacionPruebas
{
    private static readonly DateOnly NacidoEn2016 = new(2016, 7, 15);

    private readonly FabricaApi _fabrica;

    public RegistroDirectoUbicacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_jugador_queda_en_la_categoria_activa_de_su_anio_al_registrarse_sin_que_nadie_haga_nada()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2016);

        var (cliente, jugador) = await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.JUGADOR, NacidoEn2016);

        Assert.Equal(categoriaId, jugador.CategoriaId);
        Assert.Contains(jugador.Id, (await e.DetalleAsync(categoriaId)).Lista("jugadores").Ids("usuarioRolId"));
        Assert.DoesNotContain(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.False((await e.DetalleAsync(categoriaId)).GetProperty("sePuedeBorrar").GetBoolean());

        // Con el token del registro ya ve su categoría.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(e.MiCategoria)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sin_la_categoria_activa_de_su_anio_se_registra_igual_y_entra_en_ella_al_crearla_o_reactivarla(bool existeInactiva)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.CrearCategoriaPorApiAsync(2015);
        var inactiva = existeInactiva ? await e.Sembrar.CrearCategoriaAsync(e.Club, 2016, activa: false) : null;

        var (cliente, jugador) = await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.JUGADOR, NacidoEn2016);

        Assert.Equal(EstadoIngreso.APROBADO, jugador.EstadoIngreso);
        Assert.Null(jugador.CategoriaId);
        Assert.Contains(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/clubes/{e.Club.Id}")).StatusCode);

        var respuesta = inactiva is null
            ? await e.Presidente.PostAsync(e.Categorias, new { anio = 2016 })
            : await e.Presidente.PostAsync(e.Reactivacion(inactiva.Id));
        var categoriaId = (await respuesta.JsonAsync()).GetProperty("categoria").GetProperty("categoriaId").GetGuid();

        Assert.Equal(categoriaId, (await e.IntegranteGuardadoAsync(jugador.Id))!.CategoriaId);
        Assert.DoesNotContain(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
    }

    [Fact]
    public async Task Un_adulto_invitado_como_jugador_entra_como_jugador_y_queda_sin_categoria_si_no_existe_la_de_su_anio()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        await e.CrearCategoriaPorApiAsync(2016);

        var (_, adulto) = await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.JUGADOR, new DateOnly(1988, 3, 15));

        Assert.Equal(Rol.JUGADOR, adulto.Rol);
        Assert.Null(adulto.CategoriaId);
        Assert.Contains(adulto.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
    }

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Quien_entra_como_entrenador_o_directivo_no_tiene_categoria_ni_dato_de_jugador_y_es_candidato_a_entrenar(Rol rol)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2016);
        var sinCategoriaAntes = (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId");

        // Aunque naciera el año de una categoría activa: solo se ubica a quien entra como jugador.
        var (_, integrante) = await _fabrica.RegistrarConInvitacionAsync(e.Club, rol, NacidoEn2016);

        Assert.Equal(rol, integrante.Rol);
        Assert.Null(integrante.CategoriaId);
        Assert.Empty((await e.DetalleAsync(categoriaId)).Lista("jugadores"));
        Assert.Equal(sinCategoriaAntes, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.Empty(await e.Presidente.ListaAsync(e.Retirados));
        Assert.Contains(integrante.Id, (await e.Presidente.ListaAsync(e.Candidatos(categoriaId))).Ids("usuarioRolId"));
        Assert.True((await e.DetalleAsync(categoriaId)).GetProperty("sePuedeBorrar").GetBoolean());
    }

    [Fact]
    public async Task Registrarse_mientras_se_crea_la_categoria_de_su_anio_lo_deja_siempre_en_ella()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);

        foreach (var anio in Enumerable.Range(2000, 6))
        {
            var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, rol: Rol.JUGADOR);
            var documento = Sembrador.Unico("doc");

            var respuestas = await Task.WhenAll(
                _fabrica.CrearClienteDePrueba().PostAsync(
                    "/api/invitaciones/registro",
                    EscenarioIngresos.DatosDeRegistro(token, documento, new DateOnly(anio, 7, 15))),
                e.Presidente.PostAsync(e.Categorias, new { anio }));

            Assert.All(respuestas, respuesta => Assert.True(respuesta.IsSuccessStatusCode, $"{anio}: {(int)respuesta.StatusCode}"));
            var categoriaId = (await respuestas[1].JsonAsync()).GetProperty("categoria").GetProperty("categoriaId").GetGuid();
            Assert.Equal(categoriaId, (await _fabrica.IntegranteDeDocumentoAsync(e.Club, documento)).CategoriaId);
        }
    }

    [Fact]
    public async Task Ningun_registro_pone_a_nadie_en_un_equipo()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2016);
        await e.Sembrar.CrearEquipoAsync(categoria, "A");

        var (_, jugador) = await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.JUGADOR, NacidoEn2016);
        await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.ENTRENADOR);
        await _fabrica.RegistrarConInvitacionAsync(e.Club, Rol.DIRECTIVO);

        Assert.Equal(categoria.Id, jugador.CategoriaId);
        Assert.Equal(0, await _fabrica.ConContextoAsync(contexto =>
            contexto.JugadoresEquipo.IgnoreQueryFilters().CountAsync(fila => fila.ClubId == e.Club.Id)));
    }
}
