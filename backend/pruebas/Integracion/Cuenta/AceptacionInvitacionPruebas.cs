using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Aceptar una invitación con una cuenta que ya existe (RF-010): nunca una segunda cuenta, y entra
/// directamente con el rol de la invitación. La ubicación del jugador y el club suspendido están en
/// <see cref="AceptacionDirectaPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AceptacionInvitacionPruebas
{
    private readonly FabricaApi _fabrica;

    public AceptacionInvitacionPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_misma_cuenta_queda_en_dos_clubes_con_su_rol_en_cada_uno_y_un_solo_inicio_de_sesion()
    {
        var primero = await _fabrica.Sembrador.CrearClubAsync();
        var segundo = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var original = await _fabrica.Sembrador.CrearIntegranteAsync(primero, usuario, Rol.ENTRENADOR, nombres: "Lucía");
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(segundo, usuario.Correo.ToUpperInvariant());
        var cliente = _fabrica.CrearClienteDePrueba();
        await cliente.IniciarSesionAsync(usuario.Correo);

        var respuesta = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var club = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(segundo.Id, club.GetProperty("clubId").GetGuid());
        Assert.Equal("PRESIDENTE", club.GetProperty("rol").GetString());
        Assert.Equal("Lucía", club.GetProperty("nombres").GetString());

        // La misma sesión ve ya los dos clubes, cada uno con su rol.
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync("/api/sesion"));
        var roles = sesion.GetProperty("clubes").EnumerateArray()
            .ToDictionary(c => c.GetProperty("clubId").GetGuid(), c => c.GetProperty("rol").GetString());
        Assert.Equal(2, roles.Count);
        Assert.Equal("ENTRENADOR", roles[primero.Id]);
        Assert.Equal("PRESIDENTE", roles[segundo.Id]);

        // Se copió la identidad y no se creó otra cuenta.
        var integrantes = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.UsuarioId == usuario.Id).ToListAsync());
        Assert.All(integrantes, i => Assert.Equal(original.NumeroDocumento, i.NumeroDocumento));
        Assert.All(integrantes, i => Assert.Equal(EstadoIngreso.APROBADO, i.EstadoIngreso));
        Assert.Equal(1, await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.CountAsync(u => u.CorreoNormalizado == usuario.CorreoNormalizado)));
    }

    [Fact]
    public async Task Otra_cuenta_recibe_403_y_la_invitacion_sigue_sirviendo()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (invitado, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.DIRECTIVO);
        var (intruso, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.DIRECTIVO);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, invitado.Correo);
        var delIntruso = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(intruso);
        var delInvitado = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(invitado);

        var rechazada = await delIntruso.PostAsync("/api/invitaciones/aceptacion", new { token });
        var aceptada = await delInvitado.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.Forbidden, rechazada.StatusCode);
        Assert.Equal("invitacion_de_otro_correo", await ClienteDePrueba.CodigoAsync(rechazada));
        Assert.Equal(HttpStatusCode.NotFound, (await delIntruso.GetAsync($"/api/clubes/{club.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, aceptada.StatusCode);
    }

    [Fact]
    public async Task La_invitacion_no_se_puede_usar_dos_veces_ni_sin_sesion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.DIRECTIVO);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usuario.Correo);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var sinSesion = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/aceptacion", new { token });
        var primera = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });
        var segunda = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });
        var inventada = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token = "no-existe" });

        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, segunda.StatusCode);
        Assert.Equal("invitacion_no_valida", await ClienteDePrueba.CodigoAsync(segunda));
        Assert.Equal(HttpStatusCode.Gone, inventada.StatusCode);
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Con_una_invitacion_del_club_entra_aprobada_con_el_rol_de_la_invitacion_y_conserva_lo_del_otro_club(Rol rol)
    {
        var clubA = await _fabrica.Sembrador.CrearClubAsync();
        var clubB = await _fabrica.Sembrador.CrearClubAsync();
        var usuario = await _fabrica.Sembrador.CrearCuentaAsync();
        var original = await _fabrica.Sembrador.CrearIntegranteAsync(clubA, usuario, Rol.DIRECTIVO, nombres: "Lucía");
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(clubB, usuario.Correo, rol: rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var club = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(clubB.Id, club.GetProperty("clubId").GetGuid());
        Assert.Equal(rol.ToString(), club.GetProperty("rol").GetString());
        Assert.Equal("APROBADO", club.GetProperty("estadoIngreso").GetString());
        Assert.Equal("Lucía", club.GetProperty("nombres").GetString());

        // No se creó una segunda cuenta y en el club A no perdió nada (escenario 2.8).
        Assert.Equal(1, await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.CountAsync(u => u.CorreoNormalizado == usuario.CorreoNormalizado)));
        var suyos = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.UsuarioId == usuario.Id).ToListAsync());
        Assert.Equal(2, suyos.Count);
        Assert.All(suyos, i => Assert.Equal(original.NumeroDocumento, i.NumeroDocumento));
        Assert.Equal(Rol.DIRECTIVO, suyos.Single(i => i.ClubId == clubA.Id).Rol);
        Assert.Equal(EstadoIngreso.APROBADO, suyos.Single(i => i.ClubId == clubA.Id).EstadoIngreso);

        var enB = suyos.Single(i => i.ClubId == clubB.Id);
        Assert.Equal(rol, enB.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, enB.EstadoIngreso);
        Assert.Null(enB.AprobadoEn);

        // Entra de inmediato al club nuevo, con su rol, y sigue usando el otro con normalidad.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/clubes/{clubA.Id}")).StatusCode);
        var entrar = await cliente.GetAsync($"/api/clubes/{clubB.Id}");
        Assert.Equal(HttpStatusCode.OK, entrar.StatusCode);
        Assert.Equal(rol.ToString(), (await ClienteDePrueba.LeerAsync<JsonElement>(entrar)).GetProperty("miRol").GetString());
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE, Rol.JUGADOR)]
    [InlineData(Rol.DIRECTIVO, Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR, Rol.DIRECTIVO)]
    public async Task Quien_ya_esta_en_el_club_recibe_409_con_una_invitacion_del_club_y_conserva_su_rol(Rol rol, Rol rolInvitado)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usuario.Correo, rol: rolInvitado);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ya_perteneces_al_club", await ClienteDePrueba.CodigoAsync(respuesta));
        var unico = Assert.Single(await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.UsuarioId == usuario.Id && i.ClubId == club.Id).ToListAsync()));
        Assert.Equal(integrante.Id, unico.Id);
        Assert.Equal(rol, unico.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, unico.EstadoIngreso);
        // La invitación no se gastó.
        Assert.Null(await _fabrica.ConContextoAsync(contexto => contexto.Invitaciones
            .IgnoreQueryFilters().Where(i => i.Id == invitacion.Id).Select(i => i.UsadaEn).SingleAsync()));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/clubes/{club.Id}")).StatusCode);
    }

    [Fact]
    public async Task Un_directivo_invitado_como_presidente_de_su_propio_club_queda_presidente_con_un_solo_integrante()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usuario.Correo);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await cliente.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("PRESIDENTE", (await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("rol").GetString());
        var suyos = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().Where(i => i.UsuarioId == usuario.Id && i.ClubId == club.Id).ToListAsync());
        var unico = Assert.Single(suyos);
        Assert.Equal(integrante.Id, unico.Id);
        Assert.Equal(Rol.PRESIDENTE, unico.Rol);
    }
}
