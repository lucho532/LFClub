using System.Net;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using LaPecosa.Pruebas.Integracion.Categorias;
using LaPecosa.Pruebas.Integracion.Ingresos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Hermanos;

/// <summary>
/// La familia agrega un hermano desde la ficha de un jugador aprobado: queda en espera, en la misma
/// cuenta y el mismo club, con el contacto de la cuenta y sin tocar nada más (historia 1,
/// escenarios 2 a 4; RF-001 a RF-003, RF-010 y RF-011; CE-001).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AgregarHermanoPruebas
{
    private const string Responsable = "Marta Gómez";

    private readonly FabricaApi _fabrica;

    public AgregarHermanoPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    // Escenario 1.2.
    [Fact]
    public async Task Crea_un_jugador_en_espera_de_la_misma_cuenta_y_el_mismo_club_con_su_origen()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync();
        var numero = Sembrador.Unico("ti");
        var cuerpo = EscenarioHermanos.DatosDeHermano($" {numero[..3].ToUpperInvariant()}.{numero[3..]} ", nombreResponsable: Responsable);
        cuerpo["nombres"] = "  Luis   Felipe ";

        var respuesta = await familia.PostAsync(e.Hermanos(e.Ana.Id), cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var hermano = await respuesta.JsonAsync();
        Assert.Equal("Luis Felipe", hermano.GetProperty("nombres").GetString());
        Assert.Equal("Gómez", hermano.GetProperty("apellidos").GetString());
        Assert.Equal("EN_ESPERA", hermano.GetProperty("estadoIngreso").GetString());
        Assert.False(hermano.GetProperty("retirado").GetBoolean());

        var guardados = await e.JugadoresDeLaFamiliaAsync();
        Assert.Equal(2, guardados.Count);
        var guardado = guardados[1];
        Assert.Equal(hermano.GetProperty("usuarioRolId").GetGuid(), guardado.Id);
        Assert.Equal((e.Familia.Id, e.Club.Id), (guardado.UsuarioId, guardado.ClubId));
        Assert.Equal((Rol.JUGADOR, EstadoIngreso.EN_ESPERA, true), (guardado.Rol, guardado.EstadoIngreso, guardado.Activo));
        Assert.Equal(("Luis Felipe", "Gómez"), (guardado.Nombres, guardado.Apellidos));
        Assert.Equal((TipoDocumento.TARJETA_IDENTIDAD, numero), (guardado.TipoDocumento, guardado.NumeroDocumento));
        Assert.Equal(new DateOnly(EscenarioHermanos.AnioDeAna, 3, 9), guardado.FechaNacimiento);
        Assert.Equal(e.Ana.Id, guardado.AgregadoDesdeUsuarioRolId);

        // Aunque la categoría de su año existe y está activa, no entra en ella hasta que lo aprueben (RF-012).
        Assert.Null(guardado.CategoriaId);
        Assert.Empty(guardado.Equipos);
        Assert.Null(guardado.AprobadoEn);
        Assert.Null(guardado.AprobadoPorUsuarioId);
        Assert.Null(guardado.RolDeIngreso);
        Assert.True(guardado.CreadoEn > guardados[0].CreadoEn);

        // La ficha no tiene fila hasta su primer cambio, y el primer hijo no cambió.
        Assert.False(await _fabrica.ConContextoAsync(contexto =>
            contexto.FichasJugador.IgnoreQueryFilters().AnyAsync(ficha => ficha.UsuarioRolId == guardado.Id)));
        Assert.Equal(e.CategoriaDeAna.Id, guardados[0].CategoriaId);
    }

    // Escenario 1.3 y CE-001.
    [Fact]
    public async Task Comparte_el_contacto_de_la_cuenta_que_no_cambia_y_no_se_envia_ningun_correo()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.Where(cuenta => cuenta.Id == e.Familia.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(cuenta => cuenta.NombreResponsable, Responsable)));
        var antes = await e.FamiliaGuardadaAsync();
        var correosAntes = _fabrica.Correo.Enviados.Count;
        var familia = await e.ClienteDeLaFamiliaAsync();

        // El cuerpo no lleva correo, celular ni contraseña (RF-002), y la cuenta ya tiene responsable.
        var respuesta = await familia.PostAsync(e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var hermanoId = (await respuesta.JsonAsync()).GetProperty("usuarioRolId").GetGuid();
        var despues = await e.FamiliaGuardadaAsync();
        Assert.Equal(
            (antes.Correo, antes.Celular, antes.NombreResponsable, antes.ContrasenaHash, antes.SelloSeguridad),
            (despues.Correo, despues.Celular, despues.NombreResponsable, despues.ContrasenaHash, despues.SelloSeguridad));
        Assert.Equal(correosAntes, _fabrica.Correo.Enviados.Count);

        // El presidente lo ve en la sala de espera con el contacto de la cuenta.
        var enEspera = (await JsonDeCategorias.ListaAsync(e.Presidente, EscenarioIngresos.EnEspera(e.Club)))
            .Single(ingreso => ingreso.GetProperty("usuarioRolId").GetGuid() == hermanoId);
        Assert.Equal(e.Familia.Correo, enEspera.GetProperty("correo").GetString());
        Assert.Equal("3001234567", enEspera.GetProperty("celular").GetString());
        Assert.Equal(Responsable, enEspera.GetProperty("nombreResponsable").GetString());

        // La sesión abierta de la familia sigue sirviendo: no cambió la contraseña ni el sello.
        Assert.Equal(HttpStatusCode.OK, (await familia.GetAsync("/api/sesion")).StatusCode);
    }

    // Caso límite y RF-010: no hay límite de jugadores por cuenta.
    [Fact]
    public async Task Dos_hermanos_seguidos_quedan_los_dos_en_espera_y_la_sesion_los_trae()
    {
        var e = await EscenarioHermanos.CrearAsync(_fabrica);
        var familia = await e.ClienteDeLaFamiliaAsync(e.Ana.Id);

        var primero = await familia.PostAsync(e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano(nombreResponsable: Responsable));
        var trasElPrimero = (await (await familia.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes").Single();
        var segundo = await familia.PostAsync(e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano());
        var tercero = await familia.PostAsync(e.Hermanos(e.Ana.Id), EscenarioHermanos.DatosDeHermano());

        Assert.All(new[] { primero, segundo, tercero }, respuesta => Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode));
        Assert.Equal(
            [e.Ana.Id, (await primero.JsonAsync()).GetProperty("usuarioRolId").GetGuid()],
            trasElPrimero.Lista("jugadores").Ids("usuarioRolId"));

        var guardados = await e.JugadoresDeLaFamiliaAsync();
        Assert.Equal(4, guardados.Count);
        Assert.All(guardados.Skip(1), hermano =>
        {
            Assert.Equal(EstadoIngreso.EN_ESPERA, hermano.EstadoIngreso);
            Assert.Equal(e.Ana.Id, hermano.AgregadoDesdeUsuarioRolId);
        });

        var clubDeLaSesion = (await (await familia.GetAsync("/api/sesion")).JsonAsync()).Lista("clubes").Single();
        Assert.Equal(guardados.Select(jugador => jugador.Id), clubDeLaSesion.Lista("jugadores").Ids("usuarioRolId"));
        Assert.Equal(
            ["APROBADO", "EN_ESPERA", "EN_ESPERA", "EN_ESPERA"],
            clubDeLaSesion.Lista("jugadores").Select(jugador => jugador.GetProperty("estadoIngreso").GetString()));

        // RF-014: Ana sigue con normalidad, y el responsable escrito con el primero quedó en la cuenta.
        Assert.Equal(HttpStatusCode.OK, (await familia.GetAsync(e.InicioDelClub)).StatusCode);
        Assert.Equal(Responsable, (await e.FamiliaGuardadaAsync()).NombreResponsable);
    }
}
