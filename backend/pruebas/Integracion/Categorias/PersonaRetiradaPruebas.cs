using System.Net;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Categorias;

/// <summary>
/// El correo y el documento de un jugador retirado siguen ocupados en su club: no se le invita ni
/// se le registra de nuevo, se le reincorpora (RF-046; historia 6, escenario 7). Y quien acepta una
/// invitación de presidente siendo jugador del club deja de tener categoría, equipos y retiro
/// (RF-012, RF-041). Van aquí, y no en las pruebas de invitaciones de la 002, para que esos archivos
/// no superen las 250 líneas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class PersonaRetiradaPruebas
{
    private readonly FabricaApi _fabrica;

    public PersonaRetiradaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Invitar_al_correo_de_un_jugador_retirado_responde_409_y_no_envia_nada()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var correo = await CorreoDeAsync(e.IntegranteJugador.UsuarioId);
        await e.Sembrar.RetirarAsync(e.IntegranteJugador, e.IntegrantePresidente);

        var respuesta = await e.Presidente.PostAsync($"/api/clubes/{e.Club.Id}/invitaciones", new { correo, rol = "JUGADOR" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("persona_retirada", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Contains("reincorporar", await ClienteDePrueba.TituloAsync(respuesta), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _fabrica.Correo.Contar("invitacion", correo));
        Assert.Empty(await e.Presidente.ListaAsync($"/api/clubes/{e.Club.Id}/invitaciones"));
    }

    [Fact]
    public async Task Registrarse_con_el_documento_de_un_retirado_dice_que_esta_retirado_y_con_el_de_un_activo_que_esta_repetido()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (_, retirado) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015);
        await e.Sembrar.RetirarAsync(retirado, e.IntegrantePresidente);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, rol: Rol.JUGADOR);
        var cliente = _fabrica.CrearClienteDePrueba();

        var conElDelRetirado = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, retirado.NumeroDocumento));
        var conElDeUnActivo = await cliente.PostAsync("/api/invitaciones/registro", Datos(token, e.IntegranteJugador.NumeroDocumento));

        Assert.Equal(HttpStatusCode.Conflict, conElDelRetirado.StatusCode);
        Assert.Equal("persona_retirada", await ClienteDePrueba.CodigoAsync(conElDelRetirado));
        Assert.Equal(HttpStatusCode.Conflict, conElDeUnActivo.StatusCode);
        Assert.Equal("documento_repetido_en_club", await ClienteDePrueba.CodigoAsync(conElDeUnActivo));
    }

    [Fact]
    public async Task Una_invitacion_antigua_del_club_no_devuelve_al_club_a_un_jugador_retirado()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var correo = await CorreoDeAsync(e.IntegranteJugador.UsuarioId);
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, correo, rol: Rol.JUGADOR);
        await e.Sembrar.RetirarAsync(e.IntegranteJugador, e.IntegrantePresidente);

        var respuesta = await e.Jugador.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("persona_retirada", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.False((await e.IntegranteGuardadoAsync(e.IntegranteJugador.Id))!.Activo);

        // La invitación no se gastó.
        Assert.Null(await _fabrica.ConContextoAsync(contexto => contexto.Invitaciones
            .IgnoreQueryFilters().Where(fila => fila.Id == invitacion.Id).Select(fila => fila.UsadaEn).SingleAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Un_jugador_que_acepta_una_invitacion_de_presidente_queda_sin_categoria_sin_equipos_y_activo(bool estabaRetirado)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoria = await e.Sembrar.CrearCategoriaAsync(e.Club, 2015);
        var equipo = await e.Sembrar.CrearEquipoAsync(categoria, "A");
        var (usuario, jugador) = await e.Sembrar.CrearJugadorAsync(e.Club, 2015, categoria);
        await e.Sembrar.PonerEnEquipoAsync(jugador, equipo);
        if (estabaRetirado)
        {
            await e.Sembrar.RetirarAsync(jugador, e.IntegrantePresidente);
        }

        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, usuario.Correo);
        var suCliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(usuario);

        var respuesta = await suCliente.PostAsync("/api/invitaciones/aceptacion", new { token });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var aceptado = await respuesta.JsonAsync();
        Assert.Equal("PRESIDENTE", aceptado.GetProperty("rol").GetString());
        Assert.False(aceptado.GetProperty("retirado").GetBoolean());

        var guardado = (await e.IntegranteGuardadoAsync(jugador.Id))!;
        Assert.Equal(Rol.PRESIDENTE, guardado.Rol);
        Assert.True(guardado.Activo);
        Assert.Null(guardado.CategoriaId);
        Assert.Null(guardado.RetiradoEn);
        Assert.Null(guardado.RetiradoPorNombre);
        Assert.Empty(guardado.Equipos);

        // Ya no es jugador: no está en su categoría, ni en "Sin categoría", ni en "Retirados", y entra al club.
        Assert.Empty((await e.DetalleAsync(categoria.Id)).Lista("jugadores"));
        Assert.DoesNotContain(jugador.Id, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.Empty(await e.Presidente.ListaAsync(e.Retirados));
        Assert.Equal(HttpStatusCode.OK, (await suCliente.GetAsync(e.Categorias)).StatusCode);
    }

    private static object Datos(string token, string numeroDocumento) => new
    {
        token,
        nombres = "Ana",
        apellidos = "Pérez",
        tipoDocumento = "CEDULA_CIUDADANIA",
        numeroDocumento,
        fechaNacimiento = "1988-03-15",
        celular = "3001234567",
        contrasena = "mi-contrasena-propia",
    };

    private Task<string> CorreoDeAsync(Guid usuarioId) => _fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.Where(cuenta => cuenta.Id == usuarioId).Select(cuenta => cuenta.Correo).SingleAsync());
}
