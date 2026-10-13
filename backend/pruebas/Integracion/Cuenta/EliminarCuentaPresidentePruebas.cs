using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Cuenta.FamiliaParaEliminar;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Presidente, directivo y entrenador eliminan su cuenta; el presidente único de un club no puede y
/// se le dice cuál (spec 014, historia 3; RF-001, RF-009 y RF-011; constitución §8).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EliminarCuentaPresidentePruebas
{
    private readonly FabricaApi _fabrica;

    public EliminarCuentaPresidentePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Cuerpo() => new { contrasena = Sembrador.Contrasena };

    private async Task<int> PresidentesAsync(Guid clubId) => await _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().CountAsync(i => i.ClubId == clubId && i.Rol == Rol.PRESIDENTE));

    [Fact]
    public async Task El_presidente_unico_ve_el_club_que_lo_impide_y_no_se_elimina_nada()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (cuenta, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var resumen = await CuerpoAsync(await cliente.GetAsync(Ruta));
        var respuesta = await cliente.PostAsync(Ruta, Cuerpo());
        var problema = await CuerpoAsync(respuesta);

        Assert.False(resumen.GetProperty("puedeEliminar").GetBoolean());
        Assert.Equal(club.Nombre, resumen.GetProperty("clubesQueLoImpiden")[0].GetProperty("clubNombre").GetString());
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("presidente_unico", problema.GetProperty("codigo").GetString());
        Assert.Equal(club.Nombre, problema.GetProperty("clubes")[0].GetProperty("clubNombre").GetString());
        Assert.Equal(club.Id, problema.GetProperty("clubes")[0].GetProperty("clubId").GetGuid());
        Assert.True(await ExisteCuentaAsync(_fabrica, cuenta.Id));
        Assert.True(await ExisteIntegranteAsync(_fabrica, integrante.Id));
    }

    [Fact]
    public async Task Con_otro_presidente_en_el_club_se_elimina()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (cuenta, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.False(await ExisteIntegranteAsync(_fabrica, integrante.Id));
        Assert.Equal(1, await PresidentesAsync(club.Id));
    }

    [Fact]
    public async Task Presidente_unico_en_un_club_y_no_en_el_otro_queda_bloqueado_solo_por_el_primero()
    {
        var clubA = await _fabrica.Sembrador.CrearClubAsync();
        var clubB = await _fabrica.Sembrador.CrearClubAsync();
        var cuenta = await _fabrica.Sembrador.CrearCuentaAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(clubA, cuenta, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(clubB, cuenta, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(clubB, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var problema = await CuerpoAsync(await cliente.PostAsync(Ruta, Cuerpo()));

        var clubes = problema.GetProperty("clubes");
        Assert.Equal(1, clubes.GetArrayLength());
        Assert.Equal(clubA.Id, clubes[0].GetProperty("clubId").GetGuid());
        Assert.True(await ExisteCuentaAsync(_fabrica, cuenta.Id));
    }

    [Fact]
    public async Task Dos_presidentes_que_se_eliminan_a_la_vez_dejan_al_club_con_uno()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (cuentaUno, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (cuentaDos, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var uno = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaUno);
        var dos = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaDos);

        var respuestas = await Task.WhenAll(uno.PostAsync(Ruta, Cuerpo()), dos.PostAsync(Ruta, Cuerpo()));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await PresidentesAsync(club.Id));
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    public async Task Directivo_y_entrenador_eliminan_y_desaparecen_de_asignaciones(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        var (cuenta, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        await _fabrica.Categorias.AsignarEntrenadorAsync(categoria, integrante);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.False(await ExisteCuentaAsync(_fabrica, cuenta.Id));
        var asignaciones = await _fabrica.ConContextoAsync(c => c.AsignacionesEntrenadorCategoria
            .IgnoreQueryFilters().CountAsync(a => a.UsuarioRolId == integrante.Id));
        Assert.Equal(0, asignaciones);
    }

    [Fact]
    public async Task La_cuenta_desarrollador_recibe_404_en_la_lectura_y_en_la_eliminacion()
    {
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var lectura = await cliente.GetAsync(Ruta);
        var eliminacion = await cliente.PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.NotFound, lectura.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, eliminacion.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(eliminacion));
    }
}
