using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Plataforma;

/// <summary>Quitar el rol a un presidente (constitución §8, RF-019a): nunca al último.</summary>
[Collection(ColeccionApi.Nombre)]
public class RetiroPresidentePruebas
{
    private readonly FabricaApi _fabrica;

    public RetiroPresidentePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Con_dos_presidentes_asignar_directivo_deja_ese_unico_rol()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), new { accion = "ASIGNAR_ROL", rolNuevo = "DIRECTIVO" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Single(detalle.GetProperty("presidentes").EnumerateArray());
        var roles = await RolesAsync(usuario.Id);
        Assert.Equal([Rol.DIRECTIVO], roles);
    }

    [Fact]
    public async Task Eliminar_del_club_impide_entrar_a_ese_club_y_conserva_los_demas()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, usuario, Rol.ENTRENADOR);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delRetirado = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await delRetirado.GetAsync("/api/sesion"));
        var unico = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal(otroClub.Id, unico.GetProperty("clubId").GetGuid());
    }

    [Fact]
    public async Task Si_era_su_unico_club_la_cuenta_deja_de_existir()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delRetirado = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(cuenta => cuenta.Id == usuario.Id)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await delRetirado.GetAsync("/api/sesion")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await delRetirado.IniciarSesionAsync(usuario.Correo)).StatusCode);
    }

    [Theory]
    [InlineData("ASIGNAR_ROL")]
    [InlineData("ELIMINAR_DEL_CLUB")]
    public async Task Al_unico_presidente_no_se_le_puede_quitar_el_rol(string accion)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, unico) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, unico.Id), new { accion, rolNuevo = "ENTRENADOR" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ultimo_presidente", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal([Rol.PRESIDENTE], await RolesAsync(usuario.Id));
    }

    [Fact]
    public async Task Una_invitacion_pendiente_no_cuenta_como_presidente()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, unico) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearInvitacionAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, unico.Id), new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ultimo_presidente", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    [Fact]
    public async Task Dos_retiros_simultaneos_no_dejan_el_club_sin_presidente()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, uno) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (_, dos) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var primero = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var segundo = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuestas = await Task.WhenAll(
            primero.PostAsync(Ruta(club.Id, uno.Id), new { accion = "ASIGNAR_ROL", rolNuevo = "DIRECTIVO" }),
            segundo.PostAsync(Ruta(club.Id, dos.Id), new { accion = "ASIGNAR_ROL", rolNuevo = "DIRECTIVO" }));

        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Conflict);
        var presidentes = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().CountAsync(i => i.ClubId == club.Id && i.Rol == Rol.PRESIDENTE));
        Assert.Equal(1, presidentes);
    }

    [Theory]
    [InlineData("{}", "accion")]
    [InlineData("{\"accion\":\"ASIGNAR_ROL\"}", "rolNuevo")]
    [InlineData("{\"accion\":\"ASIGNAR_ROL\",\"rolNuevo\":\"PRESIDENTE\"}", "rolNuevo")]
    [InlineData("{\"accion\":\"ASIGNAR_ROL\",\"rolNuevo\":\"JUGADOR\"}", "rolNuevo")]
    public async Task Sin_elegir_que_hacer_o_con_un_rol_no_permitido_responde_400(string cuerpo, string campo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), JsonSerializer.Deserialize<JsonElement>(cuerpo));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("datos_invalidos", problema.GetProperty("codigo").GetString());
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out _));
    }

    [Fact]
    public async Task Un_integrante_que_no_es_presidente_o_de_otro_club_responde_404()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (_, directivo) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (_, deOtro) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var cuerpo = new { accion = "ELIMINAR_DEL_CLUB" };

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync(Ruta(club.Id, directivo.Id), cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync(Ruta(club.Id, deOtro.Id), cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync(Ruta(Guid.NewGuid(), deOtro.Id), cuerpo)).StatusCode);
    }

    [Fact]
    public async Task Solo_el_desarrollador_quita_el_rol()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (_, otro) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);

        var respuesta = await cliente.PostAsync(Ruta(club.Id, otro.Id), new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("solo_desarrollador", await ClienteDePrueba.CodigoAsync(respuesta));
    }

    // Los tres roles son asignables a una categoría: quien pasa a otro rol sigue entrenando (003, research §11).
    [Theory]
    [InlineData("DIRECTIVO")]
    [InlineData("ENTRENADOR")]
    public async Task Un_presidente_que_entrena_conserva_sus_categorias_al_pasar_a_otro_rol(string rolNuevo)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        var asignacion = await _fabrica.Categorias.AsignarEntrenadorAsync(categoria, retirado);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), new { accion = "ASIGNAR_ROL", rolNuevo });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(await AsignacionActivaAsync(asignacion.Id));
    }

    // Supuesto 7 de la 003: sus asignaciones se van con él y la categoría sigue contando como usada.
    [Fact]
    public async Task Eliminar_del_club_a_un_presidente_que_entrena_borra_sus_asignaciones()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, retirado) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(categoria, "A");
        var asignacion = await _fabrica.Categorias.AsignarEntrenadorAsync(categoria, retirado);
        await _fabrica.Categorias.DirigirEquipoAsync(asignacion, equipo);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id, retirado.Id), new { accion = "ELIMINAR_DEL_CLUB" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Null(await AsignacionActivaAsync(asignacion.Id));
        Assert.True(await _fabrica.ConContextoAsync(contexto => contexto.Categorias
            .IgnoreQueryFilters().Where(fila => fila.Id == categoria.Id).Select(fila => fila.Usada).SingleAsync()));
    }

    /// <summary>Si la asignación está activa, o nulo si ya no existe.</summary>
    private Task<bool?> AsignacionActivaAsync(Guid asignacionId) => _fabrica.ConContextoAsync(contexto =>
        contexto.AsignacionesEntrenadorCategoria.IgnoreQueryFilters()
            .Where(fila => fila.Id == asignacionId).Select(fila => (bool?)fila.Activa).SingleOrDefaultAsync());

    private static string Ruta(Guid clubId, Guid usuarioRolId) =>
        $"/api/plataforma/clubes/{clubId}/presidentes/{usuarioRolId}/retiro";

    private Task<List<Rol>> RolesAsync(Guid usuarioId) => _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
        .IgnoreQueryFilters().Where(i => i.UsuarioId == usuarioId).Select(i => i.Rol).ToListAsync());
}
