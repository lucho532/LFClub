using System.Net;
using System.Text.Json;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Ingresos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// Rechazar a un hermano borra solo a ese jugador: la cuenta, sus demás jugadores y la invitación
/// con la que entró el primero quedan como estaban (historia 3, escenarios 4 y 5; RF-018; CE-005;
/// constitución §20).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class RechazarHermanoPruebas
{
    private readonly FabricaApi _fabrica;

    public RechazarHermanoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static string Invitaciones(EscenarioHermanos e) => EscenarioIngresos.Invitaciones(e.Club);

    private Task<int> FilasDelJugadorAsync(Guid usuarioRolId) => _fabrica.ConContextoAsync(async contexto =>
        await contexto.UsuariosRol.IgnoreQueryFilters().CountAsync(fila => fila.Id == usuarioRolId)
        + await contexto.FichasJugador.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == usuarioRolId)
        + await contexto.DocumentosJugador.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == usuarioRolId)
        + await contexto.JugadoresEquipo.IgnoreQueryFilters().CountAsync(fila => fila.UsuarioRolId == usuarioRolId));

    // Escenario 3.4, RF-018 y CE-005.
    [Fact]
    public async Task Borra_solo_a_ese_jugador_y_deja_intactos_la_cuenta_el_primer_hijo_y_su_invitacion()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var equipo = await _fabrica.Categorias.CrearEquipoAsync(e.CategoriaDeAna, "A");
        await _fabrica.Categorias.PonerEnEquipoAsync(e.Ana, equipo);
        await _fabrica.Fichas.CrearFichaAsync(e.Ana);
        await _fabrica.Fichas.CrearDocumentoAsync(e.Ana, DocumentoPedido.CERTIFICADO_SALUD, SembradorFichas.Pdf(), "application/pdf");
        var (invitacion, _) = await _fabrica.Sembrador.CrearInvitacionAsync(
            e.Club, e.Familia.Correo, usadaEn: DateTime.UtcNow, rol: Rol.JUGADOR);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var cuentaAntes = await e.FamiliaGuardadaAsync();
        var correosAntes = _fabrica.Correo.Enviados.Count;

        var respuesta = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, hermano.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(0, await FilasDelJugadorAsync(hermano.Id));
        Assert.DoesNotContain(
            hermano.Id, (await e.Presidente.ListaAsync(EscenarioIngresos.EnEspera(e.Club)))
                .Select(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid()));

        var cuenta = await e.FamiliaGuardadaAsync();
        Assert.Equal(
            (cuentaAntes.Correo, cuentaAntes.ContrasenaHash, cuentaAntes.SelloSeguridad, cuentaAntes.Celular, cuentaAntes.NombreResponsable),
            (cuenta.Correo, cuenta.ContrasenaHash, cuenta.SelloSeguridad, cuenta.Celular, cuenta.NombreResponsable));

        var ana = Assert.Single(await e.JugadoresDeLaFamiliaAsync());
        Assert.Equal((e.Ana.Id, e.CategoriaDeAna.Id, true), (ana.Id, ana.CategoriaId, ana.Activo));
        Assert.Equal((EstadoIngreso.APROBADO, e.Ana.NumeroDocumento), (ana.EstadoIngreso, ana.NumeroDocumento));
        Assert.Equal([equipo.Id], ana.Equipos.Select(fila => fila.EquipoId));
        // Su fila, su ficha, su documento y su equipo.
        Assert.Equal(4, await FilasDelJugadorAsync(e.Ana.Id));

        // La invitación usada con la que entró Ana es el registro de su ingreso: sigue en la lista.
        Assert.Contains(
            invitacion.Id, (await e.Presidente.ListaAsync(Invitaciones(e))).Select(fila => fila.GetProperty("invitacionId").GetGuid()));
        Assert.Equal(correosAntes, _fabrica.Correo.Enviados.Count);
    }

    // Escenario 3.5.
    [Fact]
    public async Task Tras_el_rechazo_la_familia_vuelve_a_entrar_sin_elegir()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var hermano = await e.Sembrar.CrearHermanoAsync(e.Ana);
        var familia = await e.ClienteDeLaFamiliaAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await familia.GetAsync(e.InicioDelClub)).StatusCode);

        var respuesta = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, hermano.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await familia.GetAsync("/api/sesion"));
        var club = Assert.Single(sesion.GetProperty("clubes").EnumerateArray());
        Assert.Equal(0, club.GetProperty("jugadores").GetArrayLength());
        Assert.Equal(e.Ana.Id, club.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await familia.GetAsync(e.InicioDelClub)).StatusCode);

        // Quien lo tenía elegido deja de encontrarlo: no recibe datos de otro jugador.
        var conElRechazado = await familia.ElegirJugador(hermano.Id).GetAsync(e.InicioDelClub);
        Assert.Equal(HttpStatusCode.NotFound, conElRechazado.StatusCode);
        Assert.Equal("no_encontrado", await ClienteDePrueba.CodigoAsync(conElRechazado));
    }

    [Fact]
    public async Task Con_dos_hermanos_rechazar_a_uno_deja_a_los_otros_dos_y_la_lista_sigue()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Luis");
        var nico = await e.Sembrar.CrearHermanoAsync(e.Ana, nombres: "Nico");
        var familia = await e.ClienteDeLaFamiliaAsync();

        var respuesta = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, nico.Id));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal([e.Ana.Id, luis.Id], (await e.JugadoresDeLaFamiliaAsync()).Select(jugador => jugador.Id));
        var sesion = await ClienteDePrueba.LeerAsync<JsonElement>(await familia.GetAsync("/api/sesion"));
        var jugadores = sesion.GetProperty("clubes")[0].GetProperty("jugadores").EnumerateArray().ToList();
        Assert.Equal([e.Ana.Id, luis.Id], jugadores.Select(jugador => jugador.GetProperty("usuarioRolId").GetGuid()));
        Assert.Equal(HttpStatusCode.Conflict, (await familia.GetAsync(e.InicioDelClub)).StatusCode);

        // El otro hermano sigue en la sala de espera, diciendo de quién lo es.
        var enEspera = (await e.Presidente.ListaAsync(EscenarioIngresos.EnEspera(e.Club)))
            .Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == luis.Id);
        Assert.Equal($"{e.Ana.Nombres} {e.Ana.Apellidos}", enEspera.GetProperty("hermanoDe").GetString());
    }

    // A un jugador ya aprobado no se le rechaza: se le retira (§12.1.1).
    [Fact]
    public async Task Rechazar_a_un_hermano_ya_aprobado_responde_409_y_no_borra_nada()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var luis = await e.Sembrar.CrearHermanoAsync(e.Ana, EstadoIngreso.APROBADO);
        var (invitacion, _) = await _fabrica.Sembrador.CrearInvitacionAsync(
            e.Club, e.Familia.Correo, usadaEn: DateTime.UtcNow, rol: Rol.JUGADOR);

        var delHermano = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, luis.Id));
        var deAna = await e.Presidente.PostAsync(EscenarioIngresos.Rechazo(e.Club, e.Ana.Id));

        foreach (var respuesta in new[] { delHermano, deAna })
        {
            Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        }

        Assert.Equal(2, (await e.JugadoresDeLaFamiliaAsync()).Count);
        Assert.Contains(
            invitacion.Id, (await e.Presidente.ListaAsync(Invitaciones(e))).Select(fila => fila.GetProperty("invitacionId").GetGuid()));
    }
}
