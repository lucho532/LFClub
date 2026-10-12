using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// Solo el PRESIDENTE envía invitaciones de registro a su club (constitución §20). Los casos del
/// rol de la invitación están en <see cref="InvitacionConRolPruebas"/> y los de quien no es
/// presidente, con los cuatro endpoints, en <see cref="SoloPresidenteIngresosPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class InvitacionesClubPruebas
{
    private readonly FabricaApi _fabrica;

    public InvitacionesClubPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task El_presidente_invita_y_la_invitacion_queda_pendiente_a_7_dias()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (usuario, integrante) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var correo = Sembrador.CorreoUnico();

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { correo = $"  {correo.ToUpperInvariant()} ", rol = "JUGADOR" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creada = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal(correo, creada.GetProperty("correo").GetString());
        Assert.Equal("PENDIENTE", creada.GetProperty("estado").GetString());
        Assert.Equal("ENVIADO", creada.GetProperty("estadoEnvio").GetString());
        Assert.Equal($"{integrante.Nombres} {integrante.Apellidos}", creada.GetProperty("enviadaPor").GetString());
        var vigencia = creada.GetProperty("venceEn").GetDateTime() - creada.GetProperty("creadaEn").GetDateTime();
        Assert.Equal(TimeSpan.FromDays(7), vigencia);

        // El enlace enviado lleva al registro de este club, con el correo fijo.
        var token = _fabrica.Correo.UltimoToken("invitacion", correo);
        var consulta = await ClienteDePrueba.LeerAsync<JsonElement>(
            await _fabrica.CrearClienteDePrueba().PostAsync("/api/invitaciones/consulta", new { token }));
        Assert.Equal(club.Nombre, consulta.GetProperty("nombreClub").GetString());
        Assert.Equal(correo, consulta.GetProperty("correo").GetString());
        Assert.Equal("JUGADOR", consulta.GetProperty("rol").GetString());

        // La respuesta nunca contiene el token, ni al crear ni al listar.
        Assert.DoesNotContain(token, await respuesta.Content.ReadAsStringAsync());
        var lista = await cliente.GetAsync(Ruta(club.Id));
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.DoesNotContain(token, await lista.Content.ReadAsStringAsync());
        Assert.DoesNotContain("token", await lista.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var fila = Assert.Single((await ClienteDePrueba.LeerAsync<JsonElement>(lista)).EnumerateArray());
        Assert.Equal(creada.GetProperty("invitacionId").GetGuid(), fila.GetProperty("invitacionId").GetGuid());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("falta@dominio")]
    public async Task Un_correo_vacio_o_mal_escrito_responde_400_en_el_campo_y_no_envia_nada(string correo)
    {
        var (club, cliente) = await ClubConPresidenteAsync();

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" });
        var sinCampo = await cliente.PostAsync(Ruta(club.Id), new { });

        foreach (var invalida in new[] { respuesta, sinCampo })
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(invalida));
            var errores = (await ClienteDePrueba.LeerAsync<JsonElement>(invalida)).GetProperty("errores");
            Assert.True(errores.TryGetProperty("correo", out _));
        }

        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club.Id)))).EnumerateArray());
    }

    [Fact]
    public async Task No_se_invita_a_quien_ya_esta_en_el_club_ni_en_su_sala_de_espera_ni_al_desarrollador()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (entrenador, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.ENTRENADOR);
        var (enEspera, _) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var casos = new (string Correo, string Codigo)[]
        {
            (presidente.Correo, "ya_esta_en_el_club"),
            (entrenador.Correo.ToUpperInvariant(), "ya_esta_en_el_club"),
            (enEspera.Correo, "ya_esta_en_espera"),
            ($" {FabricaApi.CorreoDesarrollador.ToUpperInvariant()} ", "correo_del_desarrollador"),
        };

        var mensajes = new HashSet<string>();
        foreach (var (correo, codigo) in casos)
        {
            var respuesta = await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" });

            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
            Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
            mensajes.Add(await ClienteDePrueba.TituloAsync(respuesta));
            Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        }

        // Cada motivo tiene su propio mensaje.
        Assert.Equal(3, mensajes.Count);
        Assert.Empty((await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club.Id)))).EnumerateArray());
    }

    [Fact]
    public async Task Quien_esta_en_otro_club_si_puede_ser_invitado_a_este()
    {
        var (club, cliente) = await ClubConPresidenteAsync();
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (deOtroClub, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.DIRECTIVO);

        var respuesta = await cliente.PostAsync(Ruta(club.Id), new { correo = deOtroClub.Correo, rol = "JUGADOR" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Volver_a_invitar_al_mismo_correo_deja_una_sola_fila_y_el_enlace_anterior_deja_de_servir()
    {
        var (club, cliente) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();
        var anonimo = _fabrica.CrearClienteDePrueba();

        await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" });
        var tokenAnterior = _fabrica.Correo.UltimoToken("invitacion", correo);
        var segunda = await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" }));
        var tokenNuevo = _fabrica.Correo.UltimoToken("invitacion", correo);

        Assert.NotEqual(tokenAnterior, tokenNuevo);
        var anterior = await anonimo.PostAsync("/api/invitaciones/consulta", new { token = tokenAnterior });
        Assert.Equal(HttpStatusCode.Gone, anterior.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.PostAsync("/api/invitaciones/consulta", new { token = tokenNuevo })).StatusCode);

        var fila = Assert.Single((await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club.Id)))).EnumerateArray());
        Assert.Equal(segunda.GetProperty("invitacionId").GetGuid(), fila.GetProperty("invitacionId").GetGuid());
        Assert.Equal("PENDIENTE", fila.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Si_el_correo_no_se_puede_enviar_la_invitacion_queda_pendiente_con_el_envio_fallido()
    {
        var (club, cliente) = await ClubConPresidenteAsync();
        var correo = Sembrador.CorreoUnico();

        HttpResponseMessage respuesta;
        _fabrica.Correo.FallarEnvios = true;
        try
        {
            respuesta = await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" });
        }
        finally
        {
            _fabrica.Correo.FallarEnvios = false;
        }

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creada = await ClienteDePrueba.LeerAsync<JsonElement>(respuesta);
        Assert.Equal("FALLIDO", creada.GetProperty("estadoEnvio").GetString());
        Assert.Equal("PENDIENTE", creada.GetProperty("estado").GetString());
        var fila = Assert.Single((await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club.Id)))).EnumerateArray());
        Assert.Equal("FALLIDO", fila.GetProperty("estadoEnvio").GetString());
    }

    [Fact]
    public async Task La_lista_va_de_la_mas_reciente_a_la_mas_antigua_y_dice_el_estado_de_cada_una()
    {
        var (club, cliente) = await ClubConPresidenteAsync();
        var (usada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, usadaEn: DateTime.UtcNow, rol: Rol.JUGADOR);
        var (vencida, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, venceEn: DateTime.UtcNow.AddMinutes(-1), rol: Rol.JUGADOR);
        var (cancelada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, anuladaEn: DateTime.UtcNow, rol: Rol.JUGADOR);
        var (pendiente, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);

        var filas = (await ClienteDePrueba.LeerAsync<JsonElement>(await cliente.GetAsync(Ruta(club.Id)))).EnumerateArray().ToList();

        Assert.Equal(
            [pendiente.Id, cancelada.Id, vencida.Id, usada.Id],
            filas.Select(fila => fila.GetProperty("invitacionId").GetGuid()).ToList());
        Assert.Equal(
            ["PENDIENTE", "CANCELADA", "VENCIDA", "USADA"],
            filas.Select(fila => fila.GetProperty("estado").GetString()).ToList());
    }

    [Theory]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.JUGADOR)]
    public async Task Quien_no_es_presidente_recibe_403_sin_datos_al_listar_y_al_invitar(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (invitada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var (usuario, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);
        var sinSesion = _fabrica.CrearClienteDePrueba();
        var correo = Sembrador.CorreoUnico();

        var listar = await cliente.GetAsync(Ruta(club.Id));
        var invitar = await cliente.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" });

        foreach (var respuesta in new[] { listar, invitar })
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            Assert.DoesNotContain(invitada.Correo, await respuesta.Content.ReadAsStringAsync());
        }

        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.GetAsync(Ruta(club.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinSesion.PostAsync(Ruta(club.Id), new { correo, rol = "JUGADOR" })).StatusCode);
    }

    private static string Ruta(Guid clubId) => $"/api/clubes/{clubId}/invitaciones";

    private async Task<(Club Club, ClienteDePrueba Cliente)> ClubConPresidenteAsync()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        return (club, await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente));
    }
}
