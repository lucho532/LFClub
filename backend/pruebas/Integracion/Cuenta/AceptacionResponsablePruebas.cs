using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using LFClub.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Un JUGADOR menor de 18 años no entra a un club sin responsable, tampoco al aceptar una
/// invitación con una cuenta que ya existe: si la cuenta no lo tiene, la aceptación lo exige y lo
/// guarda; en cualquier otro caso no pide nada (RF-026; escenario 2.12). Es la continuación de
/// <see cref="AceptacionDirectaPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AceptacionResponsablePruebas
{
    private const string Aceptacion = "/api/invitaciones/aceptacion";
    private const string Consulta = "/api/invitaciones/consulta";

    private readonly FabricaApi _fabrica;

    public AceptacionResponsablePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Un_menor_sin_responsable_no_entra_como_jugador_hasta_que_lo_indica(string? enviado)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (cuenta, _) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, 2016);
        var (invitacion, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, cuenta.Correo, rol: Rol.JUGADOR);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var consulta = await (await _fabrica.CrearClienteDePrueba().PostAsync(Consulta, new { token })).JsonAsync();
        var sinResponsable = await cliente.PostAsync(Aceptacion, new { token, nombreResponsable = enviado });

        Assert.True(consulta.GetProperty("faltaResponsable").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, sinResponsable.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(sinResponsable));
        Assert.True((await sinResponsable.JsonAsync()).GetProperty("errores").TryGetProperty("nombreResponsable", out _));

        // No entró al club y la invitación no se gastó.
        Assert.False(await EstaEnElClubAsync(cuenta, e.Club));
        Assert.Null(await _fabrica.ConContextoAsync(contexto => contexto.Invitaciones
            .IgnoreQueryFilters().Where(fila => fila.Id == invitacion.Id).Select(fila => fila.UsadaEn).SingleAsync()));

        var conResponsable = await cliente.PostAsync(Aceptacion, new { token, nombreResponsable = "  Marta   Gómez " });

        Assert.Equal(HttpStatusCode.Created, conResponsable.StatusCode);
        Assert.True(await EstaEnElClubAsync(cuenta, e.Club));
        Assert.Equal("Marta Gómez", await ResponsableAsync(cuenta));
    }

    [Fact]
    public async Task Un_responsable_de_mas_de_160_caracteres_se_rechaza()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (cuenta, _) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, 2016);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, cuenta.Correo, rol: Rol.JUGADOR);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var respuesta = await cliente.PostAsync(Aceptacion, new { token, nombreResponsable = new string('a', 161) });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.False(await EstaEnElClubAsync(cuenta, e.Club));
        Assert.Null(await ResponsableAsync(cuenta));
    }

    [Theory]
    [InlineData(Rol.JUGADOR, 1990)]
    [InlineData(Rol.ENTRENADOR, 2016)]
    [InlineData(Rol.DIRECTIVO, 2016)]
    public async Task A_un_adulto_o_a_quien_no_entra_como_jugador_no_se_le_pide_y_lo_que_envie_se_ignora(Rol rol, int anioNacimiento)
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (cuenta, _) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, anioNacimiento);
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, cuenta.Correo, rol: rol);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var consulta = await (await _fabrica.CrearClienteDePrueba().PostAsync(Consulta, new { token })).JsonAsync();
        var respuesta = await cliente.PostAsync(Aceptacion, new { token, nombreResponsable = "Marta Gómez" });

        Assert.False(consulta.GetProperty("faltaResponsable").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Null(await ResponsableAsync(cuenta));
    }

    [Fact]
    public async Task A_un_menor_cuya_cuenta_ya_tiene_responsable_no_se_le_pide_y_lo_conserva()
    {
        var e = await EscenarioCategorias.CrearAsync(_fabrica);
        var (cuenta, _) = await e.Sembrar.CrearJugadorAsync(e.OtroClub, 2016);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.Where(usuario => usuario.Id == cuenta.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(usuario => usuario.NombreResponsable, "Marta Gómez")));
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(e.Club, cuenta.Correo, rol: Rol.JUGADOR);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var consulta = await (await _fabrica.CrearClienteDePrueba().PostAsync(Consulta, new { token })).JsonAsync();
        var respuesta = await cliente.PostAsync(Aceptacion, new { token, nombreResponsable = "Otra Persona" });

        Assert.False(consulta.GetProperty("faltaResponsable").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("Marta Gómez", await ResponsableAsync(cuenta));
    }

    private Task<bool> EstaEnElClubAsync(Usuario cuenta, Club club) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters()
            .AnyAsync(integrante => integrante.UsuarioId == cuenta.Id && integrante.ClubId == club.Id));

    private Task<string?> ResponsableAsync(Usuario cuenta) => _fabrica.ConContextoAsync(contexto =>
        contexto.Usuarios.Where(usuario => usuario.Id == cuenta.Id).Select(usuario => usuario.NombreResponsable).SingleAsync());
}
