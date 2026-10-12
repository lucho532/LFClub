using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Quien ya tiene cuenta y acepta una invitación del club entra directamente con el rol de la
/// invitación y, si es de JUGADOR, queda ubicado como quien se registra (RF-010 a RF-012 y RF-021;
/// escenario 2.8). Es la continuación de <see cref="AceptacionInvitacionPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AceptacionDirectaPruebas
{
    private const string Aceptacion = "/api/invitaciones/aceptacion";

    private readonly FabricaApi _fabrica;

    public AceptacionDirectaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Quien_acepta_una_invitacion_de_jugador_queda_en_la_categoria_de_su_anio_o_sin_categoria(bool existeLaCategoria)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = existeLaCategoria ? await e.CrearCategoriaPorApiAsync(2016) : (Guid?)null;
        var (cuenta, enOtroClub) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, 2016);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.Where(usuario => usuario.Id == cuenta.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(usuario => usuario.NombreResponsable, "Marta Gómez")));

        var (respuesta, nuevo) = await AceptarAsync(e.Club, cuenta, Rol.JUGADOR);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("APROBADO", (await respuesta.JsonAsync()).GetProperty("estadoIngreso").GetString());
        Assert.Equal(Rol.JUGADOR, nuevo.Rol);
        Assert.Equal(enOtroClub.FechaNacimiento, nuevo.FechaNacimiento);
        Assert.Equal(categoriaId, nuevo.CategoriaId);
        var sinCategoria = (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId");
        Assert.Equal(!existeLaCategoria, sinCategoria.Contains(nuevo.Id));

        // No pasa por la sala de espera, conserva el responsable que ya tenía y en su otro club no cambia nada.
        Assert.Empty(await e.Presidente.ListaAsync($"/api/clubes/{e.Club.Id}/ingresos/en-espera"));
        Assert.Empty(await e.Presidente.ListaAsync($"/api/clubes/{e.Club.Id}/ingresos/aprobados"));
        Assert.Equal("Marta Gómez", await _fabrica.ConContextoAsync(contexto =>
            contexto.Usuarios.Where(usuario => usuario.Id == cuenta.Id).Select(usuario => usuario.NombreResponsable).SingleAsync()));
        var intacto = (await e.IntegranteGuardadoAsync(enOtroClub.Id))!;
        Assert.Equal(Rol.JUGADOR, intacto.Rol);
        Assert.Null(intacto.CategoriaId);
    }

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Quien_acepta_una_invitacion_de_entrenador_o_directivo_no_tiene_categoria_ni_aparece_como_jugador(Rol rol)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var categoriaId = await e.CrearCategoriaPorApiAsync(2016);
        // Nació el año de una categoría activa y en su otro club es jugador: aquí no lo es.
        var (cuenta, _) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, 2016);
        var sinCategoriaAntes = (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId");

        var (respuesta, nuevo) = await AceptarAsync(e.Club, cuenta, rol);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(rol, nuevo.Rol);
        Assert.Equal(EstadoIngreso.APROBADO, nuevo.EstadoIngreso);
        Assert.Null(nuevo.CategoriaId);
        Assert.Null(nuevo.AprobadoEn);
        Assert.Empty((await e.DetalleAsync(categoriaId)).Lista("jugadores"));
        Assert.Equal(sinCategoriaAntes, (await e.Presidente.ListaAsync(e.SinCategoria)).Ids("usuarioRolId"));
        Assert.Contains(nuevo.Id, (await e.Presidente.ListaAsync(e.Candidatos(categoriaId))).Ids("usuarioRolId"));
    }

    [Fact]
    public async Task En_un_club_suspendido_la_aceptacion_se_completa_y_al_entrar_ve_el_aviso_de_incidencia()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync(estado: EstadoClub.SUSPENDIDO);
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var (cuenta, _) = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, Rol.ENTRENADOR);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, cuenta.Correo, rol: Rol.DIRECTIVO);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PostAsync(Aceptacion, new { token });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var aceptado = await respuesta.JsonAsync();
        Assert.Equal("DIRECTIVO", aceptado.GetProperty("rol").GetString());
        Assert.Equal("APROBADO", aceptado.GetProperty("estadoIngreso").GetString());
        var entrar = await cliente.GetAsync($"/api/clubes/{club.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, entrar.StatusCode);
        Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(entrar));
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/clubes/{otroClub.Id}")).StatusCode);
    }

    /// <summary>Siembra una invitación de ese rol al correo de la cuenta y la acepta con su sesión.</summary>
    private async Task<(HttpResponseMessage Respuesta, UsuarioRol Nuevo)> AceptarAsync(Club club, Usuario cuenta, Rol rol)
    {
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, cuenta.Correo, rol: rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PostAsync(Aceptacion, new { token });
        var nuevo = await _fabrica.ConContextoAsync(contexto => contexto.UsuariosRol
            .IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(integrante => integrante.UsuarioId == cuenta.Id && integrante.ClubId == club.Id));
        return (respuesta, nuevo);
    }
}
