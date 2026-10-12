using System.Net;
using System.Text.Json;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// El registro pide el nombre del padre, madre o responsable solamente con una invitación de
/// JUGADOR: obligatorio si es menor de 18 años y opcional si es adulto. Con cualquier otro rol,
/// también PRESIDENTE, ni se exige ni se guarda (RF-013; escenario 2.7). Es la continuación de
/// <see cref="RegistroDirectoPruebas"/>, separada para no pasar de 250 líneas.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RegistroDirectoResponsablePruebas
{
    private const string Registro = "/api/invitaciones/registro";

    private readonly FabricaApi _fabrica;

    public RegistroDirectoResponsablePruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Un_jugador_menor_sin_responsable_responde_400_y_no_gasta_la_invitacion_y_con_responsable_se_registra()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var cliente = _fabrica.CrearClienteDePrueba();
        var haceDiezAnos = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        var documento = Sembrador.Unico("doc");

        var sinResponsable = await cliente.PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token, documento, haceDiezAnos));
        var demasiadoLargo = await cliente.PostAsync(
            Registro, EscenarioIngresos.DatosDeRegistro(token, documento, haceDiezAnos, new string('a', 161)));
        var conResponsable = await cliente.PostAsync(
            Registro, EscenarioIngresos.DatosDeRegistro(token, documento, haceDiezAnos, "  Marta   Gómez "));

        foreach (var invalida in new[] { sinResponsable, demasiadoLargo })
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
            Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(invalida));
            Assert.True((await ClienteDePrueba.LeerAsync<JsonElement>(invalida)).GetProperty("errores").TryGetProperty("nombreResponsable", out _));
        }

        Assert.Equal(HttpStatusCode.Created, conResponsable.StatusCode);
        var integrante = await _fabrica.IntegranteDeDocumentoAsync(club, documento);
        Assert.Equal(EstadoIngreso.APROBADO, integrante.EstadoIngreso);
        Assert.Equal("Marta Gómez", (await _fabrica.CuentaAsync(integrante.UsuarioId)).NombreResponsable);
    }

    [Fact]
    public async Task Un_jugador_adulto_se_registra_sin_responsable()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, token) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: Rol.JUGADOR);
        var documento = Sembrador.Unico("doc");

        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(token, documento));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var integrante = await _fabrica.IntegranteDeDocumentoAsync(club, documento);
        Assert.Equal(Rol.JUGADOR, integrante.Rol);
        Assert.Null((await _fabrica.CuentaAsync(integrante.UsuarioId)).NombreResponsable);
    }

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    [InlineData(Rol.PRESIDENTE)]
    public async Task Fuera_del_jugador_el_responsable_no_se_exige_ni_se_guarda_aunque_llegue_en_el_cuerpo(Rol rol)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (_, sinEnviarlo) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: rol);
        var (_, enviandolo) = await _fabrica.Sembrador.CrearInvitacionAsync(club, rol: rol);
        var haceDiezAnos = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        var cliente = _fabrica.CrearClienteDePrueba();
        var documentos = new[] { Sembrador.Unico("doc"), Sembrador.Unico("doc") };

        // Ni siendo menor se le exige, y uno de más de 160 caracteres tampoco es un error: se ignora.
        var sin = await cliente.PostAsync(Registro, EscenarioIngresos.DatosDeRegistro(sinEnviarlo, documentos[0], haceDiezAnos));
        var con = await cliente.PostAsync(
            Registro, EscenarioIngresos.DatosDeRegistro(enviandolo, documentos[1], nombreResponsable: new string('a', 161)));

        Assert.Equal(HttpStatusCode.Created, sin.StatusCode);
        Assert.Equal(HttpStatusCode.Created, con.StatusCode);
        foreach (var documento in documentos)
        {
            var integrante = await _fabrica.IntegranteDeDocumentoAsync(club, documento);
            Assert.Equal(rol, integrante.Rol);
            Assert.Null((await _fabrica.CuentaAsync(integrante.UsuarioId)).NombreResponsable);
        }
    }
}
