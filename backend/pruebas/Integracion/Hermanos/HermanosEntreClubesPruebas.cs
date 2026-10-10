using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Los hermanos frente a varios clubes, al estado del club y a los borrados: el hermano es solo del
/// club donde se agregó, el club suspendido o dado de baja no deja agregar ni entrar, y eliminar un
/// club se lleva a sus hermanos sin tocar los de otro (casos límite de la spec; RF-028;
/// constitución §7.1, §7.4 y §20).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class HermanosEntreClubesPruebas
{
    private readonly FabricaApi _fabrica;

    public HermanosEntreClubesPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static Dictionary<string, object?> Datos() =>
        EscenarioHermanos.DatosDeHermano(nombreResponsable: "Marta Gómez");

    private Task<int> CambiarEstadoAsync(Club club, EstadoClub estado) => _fabrica.ConContextoAsync(contexto =>
        contexto.Clubes.Where(fila => fila.Id == club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(fila => fila.Estado, estado)));

    private Task<List<Guid>> IntegrantesAsync(Club club) => _fabrica.ConContextoAsync(contexto =>
        contexto.UsuariosRol.IgnoreQueryFilters().Where(fila => fila.ClubId == club.Id)
            .OrderBy(fila => fila.Id).Select(fila => fila.Id).ToListAsync());

    // RF-028 y caso límite: el hermano se agrega solo en el club de la ficha.
    [Fact]
    public async Task El_hermano_es_solo_del_club_donde_se_agrego()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(e.OtroClub, e.Familia, Rol.JUGADOR);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var inicioDelOtro = $"/api/clubes/{e.OtroClub.Id}";

        var agregado = await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos());

        Assert.Equal(HttpStatusCode.Created, agregado.StatusCode);
        var hermanoId = (await agregado.JsonAsync()).GetProperty("usuarioRolId").GetGuid();
        var clubes = (await (await familia.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes");
        var delClub = clubes.Single(club => club.GetProperty("clubId").GetGuid() == e.Club.Id);
        var delOtro = clubes.Single(club => club.GetProperty("clubId").GetGuid() == e.OtroClub.Id);
        Assert.Equal([e.Ana.Id, hermanoId], delClub.Lista("jugadores").Ids("usuarioRolId"));
        Assert.Empty(delOtro.Lista("jugadores"));
        Assert.Equal(enElOtro.Id, delOtro.GetProperty("usuarioRolId").GetGuid());

        // En el otro club entra sin elegir, y un jugador del primero no vale como elegido en él.
        Assert.Equal(HttpStatusCode.Conflict, (await familia.GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await familia.GetAsync(inicioDelOtro)).StatusCode);
        foreach (var deOtroClub in new[] { hermanoId, e.Ana.Id })
        {
            var respuesta = await familia.ElegirJugador(deOtroClub).GetAsync(inicioDelOtro);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        // El presidente del otro club no lo ve en su sala de espera ni puede aprobarlo.
        var suSalaDeEspera = await e.Base.PresidenteDeOtroClub.GetAsync($"{inicioDelOtro}/ingresos/en-espera");
        Assert.Equal(HttpStatusCode.OK, suSalaDeEspera.StatusCode);
        Assert.DoesNotContain(hermanoId.ToString(), await suSalaDeEspera.Content.ReadAsStringAsync());
        var aprobacion = await e.Base.PresidenteDeOtroClub.PostAsync($"{inicioDelOtro}/ingresos/{hermanoId}/aprobacion");
        Assert.Equal(HttpStatusCode.NotFound, aprobacion.StatusCode);
        Assert.Single(await e.JugadoresDeLaFamiliaAsync(e.OtroClub));
    }

    // Caso límite: el club se suspende mientras hay un hermano en espera.
    [Fact]
    public async Task En_un_club_suspendido_la_familia_no_agrega_ni_entra_y_el_hermano_sigue_en_espera_al_reactivarlo()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var familia = await e.ClienteDeLaFamiliaAsync();
        await CambiarEstadoAsync(e.Club, EstadoClub.SUSPENDIDO);

        foreach (var elegido in new[] { e.Ana.Id, hermano.Id })
        {
            familia.ElegirJugador(elegido);
            foreach (var respuesta in new[]
            {
                await familia.PostAsync(e.Hermanos(elegido), Datos()), await familia.GetAsync(e.InicioDelClub),
            })
            {
                Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
                Assert.Equal("club_suspendido", await ClienteDePrueba.CodigoAsync(respuesta));
            }
        }

        // El presidente sigue entrando y lo ve en la sala de espera.
        Assert.Contains(
            hermano.Id.ToString(),
            await (await e.Presidente.GetAsync($"{e.InicioDelClub}/ingresos/en-espera")).Content.ReadAsStringAsync());
        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);

        await CambiarEstadoAsync(e.Club, EstadoClub.ACTIVO);

        Assert.Equal(EstadoIngreso.EN_ESPERA, (await e.JugadoresDeLaFamiliaAsync())[1].EstadoIngreso);
        Assert.Equal(HttpStatusCode.OK, (await familia.ElegirJugador(e.Ana.Id).GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await familia.PostAsync(e.Hermanos(e.Ana.Id), Datos())).StatusCode);
    }

    [Fact]
    public async Task En_un_club_dado_de_baja_nadie_agrega_ni_aprueba_a_un_hermano()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var conAna = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);
        await CambiarEstadoAsync(e.Club, EstadoClub.DADO_DE_BAJA);

        HttpResponseMessage[] respuestas =
        [
            await conAna.PostAsync(e.Hermanos(e.Ana.Id), Datos()),
            await conAna.GetAsync(e.InicioDelClub),
            await e.Presidente.GetAsync($"{e.InicioDelClub}/ingresos/en-espera"),
            await e.Presidente.PostAsync($"{e.InicioDelClub}/ingresos/{hermano.Id}/aprobacion"),
            await e.Presidente.PostAsync($"{e.InicioDelClub}/ingresos/{hermano.Id}/rechazo"),
        ];

        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal("club_dado_de_baja", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(EstadoIngreso.EN_ESPERA, (await e.JugadoresDeLaFamiliaAsync())[1].EstadoIngreso);
    }

    // §7.4 y §12.4: eliminar el club borra a sus hermanos, también al que está en espera, y con él
    // la cuenta que solo pertenecía a ese club; no toca a los hermanos de otro club.
    [Fact]
    public async Task Eliminar_el_club_borra_a_los_hermanos_y_la_cuenta_que_solo_era_suya_y_no_toca_otro_club()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await e.Sembrar.CrearHermanoAsync(e.Ana);
        await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, categoria: e.CategoriaDeAna);
        var otro = await EscenarioHermanos.CrearAsync(_fabrica);
        await otro.Sembrar.CrearHermanoAsync(otro.Ana);
        await otro.Sembrar.CrearHermanoAsync(otro.Ana, EstadoIngreso.APROBADO);
        var delOtroAntes = await IntegrantesAsync(otro.Club);
        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();
        await CambiarEstadoAsync(e.Club, EstadoClub.DADO_DE_BAJA);

        var respuesta = await desarrollador.PostAsync(
            $"/api/plataforma/clubes/{e.Club.Id}/eliminacion", new { nombreDeConfirmacion = e.Club.Nombre });

        Assert.True(respuesta.IsSuccessStatusCode, $"Eliminación: {(int)respuesta.StatusCode}");
        Assert.Empty(await IntegrantesAsync(e.Club));
        Assert.False(await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.AnyAsync(cuenta => cuenta.Id == e.Familia.Id)));

        Assert.Equal(delOtroAntes, await IntegrantesAsync(otro.Club));
        Assert.Equal(3, (await otro.JugadoresDeLaFamiliaAsync()).Count);
        var deLaOtraFamilia = await otro.ClienteDeLaFamiliaAsync(otro.Ana.Id);
        Assert.Equal(HttpStatusCode.OK, (await deLaOtraFamilia.GetAsync(otro.InicioDelClub)).StatusCode);
    }

    // Caso límite: rechazan o retiran al jugador que la familia tiene elegido.
    [Fact]
    public async Task Si_rechazan_o_retiran_al_jugador_elegido_la_siguiente_peticion_no_entrega_datos_de_otro()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var enEspera = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Luis");
        var aprobado = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO, nombres: "Mara", categoria: e.CategoriaDeAna);
        var conElEnEspera = await e.ClienteDeLaFamiliaAsync(enEspera.Id);
        var conElAprobado = await e.ClienteDeLaFamiliaAsync(aprobado.Id);
        Assert.Equal(HttpStatusCode.OK, (await conElAprobado.GetAsync(e.Base.MiCategoria)).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await e.Presidente.PostAsync($"{e.InicioDelClub}/ingresos/{enEspera.Id}/rechazo")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await e.Presidente.PostAsync(e.Base.Retiro(aprobado.Id))).StatusCode);

        var delRechazado = await conElEnEspera.GetAsync(e.InicioDelClub);
        var delRetirado = await conElAprobado.GetAsync(e.Base.MiCategoria);

        Assert.Equal(HttpStatusCode.NotFound, delRechazado.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(delRechazado));
        Assert.Equal(HttpStatusCode.Forbidden, delRetirado.StatusCode);
        Assert.Equal("integrante_retirado", await ClienteDePrueba.CodigoAsync(delRetirado));
        foreach (var respuesta in new[] { delRechazado, delRetirado })
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(e.Club.Nombre, cuerpo);
            Assert.DoesNotContain(e.Ana.Id.ToString(), cuerpo);
        }

        // La sesión, al recargarla, ya no trae al rechazado y marca al retirado.
        var jugadores = (await (await conElAprobado.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes").Single().Lista("jugadores");
        Assert.Equal([e.Ana.Id, aprobado.Id], jugadores.Ids("usuarioRolId"));
        Assert.Equal([false, true], jugadores.Select(jugador => jugador.GetProperty("retirado").GetBoolean()));
    }
}
