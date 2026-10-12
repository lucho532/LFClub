using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Toda invitación del club lleva el rol con el que entra la persona, que elige el PRESIDENTE:
/// JUGADOR, ENTRENADOR o DIRECTIVO (constitución §8 y §12.1; RF-001 a RF-004 y RF-019; historia 1).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class InvitacionConRolPruebas
{
    private readonly FabricaApi _fabrica;

    public InvitacionConRolPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task El_presidente_invita_con_cada_rol_y_la_invitacion_queda_pendiente_con_ese_rol(Rol rol)
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();

        var respuesta = await cliente.InvitarAsync(club, correo, rol);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creada = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(rol.ToString(), creada.GetProperty("rol").GetString());
        Assert.Equal("PENDIENTE", creada.GetProperty("estado").GetString());
        Assert.Equal(rol, (await InvitacionAsync(creada.GetProperty("invitacionId").GetGuid())).Rol);

        var fila = Assert.Single(await cliente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
        Assert.Equal(correo, fila.GetProperty("correo").GetString());
        Assert.Equal(rol.ToString(), fila.GetProperty("rol").GetString());

        // El correo y el enlace dicen el mismo rol.
        Assert.Equal(rol, _fabrica.Correo.Enviados.Last(enviado => enviado.Destinatario == correo).Rol);
        Assert.Equal(rol.ToString(), await RolDelEnlaceAsync(_fabrica.Correo.UltimoToken("invitacion", correo)));
    }

    [Fact]
    public async Task Sin_rol_responde_400_en_el_campo_rol_y_no_crea_ni_envia_nada()
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();

        foreach (var cuerpo in new object[] { new { correo }, new { correo, rol = (string?)null }, new { correo, rol = "UTILERO" } })
        {
            var respuesta = await cliente.PostAsync(EscenarioIngresos.Invitaciones(club), cuerpo);

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.True((await ClienteDePrueba.LeerAsync<JsonElement>(respuesta)).GetProperty("errores").TryGetProperty("rol", out _));
        }

        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        Assert.Empty(await cliente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
    }

    [Theory]
    [InlineData(Rol.PRESIDENTE)]
    [InlineData(Rol.DESARROLLADOR)]
    public async Task Con_el_rol_presidente_o_desarrollador_responde_403_y_no_crea_ni_envia_nada(Rol rol)
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();

        var respuesta = await cliente.InvitarAsync(club, correo, rol);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("rol_no_invitable", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        Assert.Empty(await cliente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Reenviar_conserva_el_rol_de_la_invitacion(Rol rol)
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();
        var primera = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.InvitarAsync(club, correo, rol));

        // Un rol enviado al reenviar no cambia nada: el reenvío no lleva cuerpo.
        var respuesta = await cliente.PostAsync(
            EscenarioIngresos.Reenvio(club, primera.GetProperty("invitacionId").GetGuid()), new { rol = "JUGADOR" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var nueva = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(rol.ToString(), nueva.GetProperty("rol").GetString());
        Assert.Equal(rol, (await InvitacionAsync(nueva.GetProperty("invitacionId").GetGuid())).Rol);
        Assert.Equal(rol.ToString(), await RolDelEnlaceAsync(_fabrica.Correo.UltimoToken("invitacion", correo)));
    }

    [Fact]
    public async Task Invitar_de_nuevo_con_otro_rol_deja_una_sola_fila_con_el_rol_nuevo_y_el_enlace_anterior_no_sirve()
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();
        await cliente.InvitarAsync(club, correo, Rol.ENTRENADOR);
        var tokenAnterior = _fabrica.Correo.UltimoToken("invitacion", correo);

        var respuesta = await cliente.InvitarAsync(club, correo, Rol.DIRECTIVO);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var fila = Assert.Single(await cliente.ListaAsync(EscenarioIngresos.Invitaciones(club)));
        Assert.Equal("DIRECTIVO", fila.GetProperty("rol").GetString());
        Assert.Equal("PENDIENTE", fila.GetProperty("estado").GetString());
        var anonimo = _fabrica.CrearClienteDePrueba();
        Assert.Equal(
            HttpStatusCode.Gone,
            (await anonimo.PostAsync("/api/invitaciones/consulta", new { token = tokenAnterior })).StatusCode);
        Assert.Equal("DIRECTIVO", await RolDelEnlaceAsync(_fabrica.Correo.UltimoToken("invitacion", correo)));
    }

    [Theory]
    [InlineData(Rol.JUGADOR)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Un_correo_que_ya_es_integrante_del_club_se_rechaza_sea_cual_sea_el_rol(Rol rol)
    {
        var (club, cliente, _) = await ClubConPresidenteAsync();
        var (integrante, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);

        var respuesta = await cliente.InvitarAsync(club, integrante.Correo, rol);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ya_esta_en_el_club", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", integrante.Correo));
    }

    [Fact]
    public async Task Una_invitacion_usada_aparece_como_usada_con_su_rol_y_quien_la_envio()
    {
        var (club, cliente, presidente) = await ClubConPresidenteAsync();
        var cuenta = await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.AsNoTracking().SingleAsync(usuario => usuario.Id == presidente.UsuarioId));
        var (usada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(
            club, usadaEn: DateTime.UtcNow, rol: Rol.ENTRENADOR, creadaPor: cuenta);

        var fila = Assert.Single(await cliente.ListaAsync(EscenarioIngresos.Invitaciones(club)));

        Assert.Equal(usada.Id, fila.GetProperty("invitacionId").GetGuid());
        Assert.Equal("USADA", fila.GetProperty("estado").GetString());
        Assert.Equal("ENTRENADOR", fila.GetProperty("rol").GetString());
        Assert.Equal($"{presidente.Nombres} {presidente.Apellidos}", fila.GetProperty("enviadaPor").GetString());
    }

    private async Task<(Club Club, ClienteDePrueba Cliente, UsuarioRol Presidente)> ClubConPresidenteAsync()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        return (club, await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario), integrante);
    }

    private async Task<string?> RolDelEnlaceAsync(string token)
    {
        var consulta = await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token });
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        return (await ClienteDePrueba.LeerAsync<JsonElement>(consulta)).GetProperty("rol").GetString();
    }

    private Task<Invitacion> InvitacionAsync(Guid invitacionId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Invitaciones.IgnoreQueryFilters().AsNoTracking().SingleAsync(invitacion => invitacion.Id == invitacionId));
}
