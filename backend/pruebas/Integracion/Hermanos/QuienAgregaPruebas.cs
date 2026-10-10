using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Solo la cuenta de un jugador aprobado y activo agrega un hermano, y solo desde la ficha del
/// jugador con el que continúa (historia 1, escenarios 8 y 9; RF-001, RF-007, RF-008 y RF-029).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class QuienAgregaPruebas
{
    private readonly FabricaApi _fabrica;

    public QuienAgregaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static Dictionary<string, object?> Datos() =>
        EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez");

    private Task<int> IntegrantesDelClubAsync(EscenarioHermanos e) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().CountAsync(integrante => integrante.ClubId == e.Club.Id));

    // Escenario 1.9 y RF-007: ni sobre la ficha de un jugador ni sobre su propio identificador.
    [Fact]
    public async Task El_presidente_el_directivo_y_el_entrenador_reciben_403_y_el_desarrollador_404()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var antes = await IntegrantesDelClubAsync(e);
        (string Rol, ClienteDePrueba Cliente, Guid Propio)[] quienes =
        [
            ("PRESIDENTE", e.Presidente, e.Base.IntegrantePresidente.Id),
            ("DIRECTIVO", e.Directivo, e.Base.IntegranteDirectivo.Id),
            ("ENTRENADOR", e.Entrenador, e.Base.IntegranteEntrenador.Id),
        ];

        foreach (var (rol, cliente, propio) in quienes)
        {
            foreach (var desde in new[] { e.Ana.Id, propio })
            {
                var respuesta = await cliente.PostAsync(e.Hermanos(desde), Datos());

                Assert.True(respuesta.StatusCode == HttpStatusCode.Forbidden, $"{rol}: {(int)respuesta.StatusCode}");
                Assert.Equal("rol_no_autorizado", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        var delDesarrollador = await desarrollador.PostAsync(e.Hermanos(e.Ana.Id), Datos());

        Assert.Equal(HttpStatusCode.NotFound, delDesarrollador.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(delDesarrollador));
        Assert.Equal(antes, await IntegrantesDelClubAsync(e));
    }

    // Escenario 1.8 y RF-008.
    [Fact]
    public async Task No_se_agrega_un_hermano_desde_un_jugador_retirado_ni_desde_uno_en_espera()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var retirado = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, activo: false);
        var familia = await e.ClienteDeLaFamiliaAsync();

        var desdeElRetirado = await familia.ElegirJugador(retirado.Id).PostAsync(e.Hermanos(retirado.Id), Datos());
        var desdeElEnEspera = await familia.ElegirJugador(enEspera.Id).PostAsync(e.Hermanos(enEspera.Id), Datos());

        Assert.Equal(HttpStatusCode.Forbidden, desdeElRetirado.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(desdeElRetirado));
        Assert.Equal(HttpStatusCode.Forbidden, desdeElEnEspera.StatusCode);
        Assert.Equal("ingreso_en_espera", await ClienteDePrueba.CodigoAsync(desdeElEnEspera));
        Assert.Equal(3, (await e.JugadoresDeLaFamiliaAsync()).Count);

        // Una cuenta de un solo jugador, retirado: tampoco.
        var (cuentaRetirada, unico) = await _fabrica.Categorias.CrearJugadorAsync(e.Club, 2014);
        await _fabrica.Categorias.RetirarAsync(unico, e.Base.IntegrantePresidente);
        var delRetirado = await (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuentaRetirada))
            .PostAsync(e.Hermanos(unico.Id), Datos());
        Assert.Equal(HttpStatusCode.Forbidden, delRetirado.StatusCode);
    }

    // RF-029: solo desde la ficha del jugador con el que continúa.
    [Fact]
    public async Task Con_un_hijo_elegido_no_se_agrega_desde_la_ficha_de_otro_jugador()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var (_, deOtroClub) = await _fabrica.Categorias.CrearJugadorAsync(e.OtroClub, 2014);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        var antes = await IntegrantesDelClubAsync(e);
        var ajenos = new Dictionary<string, Guid>
        {
            ["su hermano"] = luis.Id, ["Beto"] = e.Beto.Id, ["otro club"] = deOtroClub.Id, ["inexistente"] = Guid.NewGuid(),
        };

        foreach (var (quien, id) in ajenos)
        {
            var respuesta = await familia.PostAsync(e.Hermanos(id), Datos());

            Assert.True(respuesta.StatusCode == HttpStatusCode.NotFound, $"{quien}: {(int)respuesta.StatusCode}");
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        // Tampoco en el club de ese otro jugador, al que la cuenta no pertenece.
        var enElOtroClub = await familia.PostAsync(EscenarioHermanos.Hermanos(e.OtroClub, deOtroClub.Id), Datos());
        Assert.Equal(HttpStatusCode.NotFound, enElOtroClub.StatusCode);
        Assert.Equal(antes, await IntegrantesDelClubAsync(e));
    }

    [Fact]
    public async Task Una_cuenta_con_varios_jugadores_y_sin_elegir_recibe_409()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var familia = await e.ClienteDeLaFamiliaAsync();

        var respuesta = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos());

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("jugador_sin_elegir", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);
    }

    // Caso límite: quien entró con el documento de un hijo agrega un hermano y sigue viendo solo a ese hijo.
    [Fact]
    public async Task En_una_sesion_iniciada_con_el_documento_de_un_hijo_se_agrega_y_se_sigue_viendo_solo_a_ese_hijo()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var cliente = _fabrica.CrearClienteDePrueba();
        Assert.Equal(HttpStatusCode.OK, (await cliente.IniciarSesionAsync(e.Ana.NumeroDocumento)).StatusCode);

        var respuesta = await cliente.PostAsync(e.Hermanos(e.Ana.Id), Datos());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var hermanoId = (await respuesta.JsonAsync()).GetProperty("usuarioRolId").GetGuid();
        var sesion = await cliente.GetAsync("/api/sesion");
        var club = (await sesion.JsonAsync()).Lista("clubes").Single();
        Assert.Empty(club.Lista("jugadores"));
        Assert.Equal(e.Ana.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.DoesNotContain(hermanoId.ToString(), await sesion.Content.ReadAsStringAsync());

        // Sin cabecera sigue entrando como Ana, y al hermano no llega ni eligiéndolo.
        var delClub = await cliente.GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.OK, delClub.StatusCode);
        Assert.Equal(e.Ana.Id, (await delClub.JsonAsync()).GetProperty("miUsuarioRolId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.ElegirJugador(hermanoId).GetAsync(e.InicioDelClub)).StatusCode);
    }
}
