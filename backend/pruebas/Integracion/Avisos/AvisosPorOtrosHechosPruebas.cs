using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Avisos;

/// <summary>
/// Solo los diez hechos de actividad generan avisos: ingresar, retirar, cambiar de categoría,
/// corregir la ficha o responder a una convocatoria no generan ninguno; quien entra a una categoría
/// no recibe lo anterior; el equipo del club y el DESARROLLADOR no tienen avisos (spec 012, historia
/// 4; RF-011, RF-028 y RF-029).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AvisosPorOtrosHechosPruebas : IDisposable
{
    private readonly FabricaApi _fabrica;

    public AvisosPorOtrosHechosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
        _fabrica.Reloj.UsarElReal();
    }

    public void Dispose() => _fabrica.Reloj.UsarElReal();

    private DateOnly Manana => _fabrica.Reloj.Hoy.AddDays(1);

    [Fact]
    public async Task Retirar_cambiar_de_categoria_corregir_la_ficha_y_responder_no_generan_avisos()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var partido = await _fabrica.Actividades.CrearPartidoAsync([e.C2014], Manana, convocados: [e.Jn(3).Integrante]);
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var antes = await _fabrica.Avisos.ContarTodosAsync();

        // Responder a la convocatoria y al entrenamiento.
        await Actividades.LeerAsync(await e.Jn(3).Cliente.PutAsync(
            e.Ruta($"/partidos/{partido.Id}/mi-respuesta"), new { respuesta = "ASISTIRA" }));
        await Actividades.LeerAsync(await e.Jn(4).Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{entrenamiento.Id}/mi-respuesta"), new { respuesta = "NO_ASISTIRA" }));
        // Corregir la ficha.
        await Actividades.LeerAsync(await e.Jn(5).Cliente.PutAsync(
            e.Ruta($"/jugadores/{e.Jn(5).Id}/ficha/identidad"),
            new
            {
                nombres = "J5",
                apellidos = "Apellido corregido",
                fechaNacimiento = e.Jn(5).Integrante.FechaNacimiento.ToString("yyyy-MM-dd"),
            }));
        // Cambiar de categoría y retirar.
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(
            e.Ruta($"/jugadores/{e.Jn(6).Id}/categoria"), new { categoriaId = e.C2015.Id }));
        var retiro = await e.P.Cliente.PostAsync(e.Ruta($"/jugadores/{e.Jn(7).Id}/retiro"));

        Assert.True(retiro.IsSuccessStatusCode);
        Assert.Equal(antes, await _fabrica.Avisos.ContarTodosAsync());
    }

    [Fact]
    public async Task Quien_cambio_de_categoria_conserva_lo_que_recibio_y_no_recibe_lo_anterior_de_la_nueva()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var de2014 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        var de2015 = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2015], Manana);
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{de2014.Id}"), Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo")));
        await Actividades.LeerAsync(await e.E2.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{de2015.Id}"), Actividades.Entrenamiento(Manana, [e.C2015], lugar: "Estadio")));
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));

        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(
            e.Ruta($"/jugadores/{e.Jn(3).Id}/categoria"), new { categoriaId = e.C2015.Id }));

        var textos = await AyudaDeAvisos.TextosAsync(e, e.Jn(3));
        Assert.Equal(["Cambió el lugar del entrenamiento: ahora en Coliseo"], textos);
        // Un jugador que ya estaba en la 2015 sí recibió el de su categoría; J3 no recibe el anterior.
        Assert.Contains("Estadio", (await AyudaDeAvisos.TextosAsync(e, e.K1)).Single());
    }

    [Fact]
    public async Task Un_jugador_retirado_deja_de_acceder_y_al_reincorporarlo_vuelve_a_ver_lo_que_le_habia_llegado()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        await Actividades.LeerAsync(await e.E1.Cliente.PutAsync(
            e.Ruta($"/entrenamientos/{entrenamiento.Id}"),
            Actividades.Entrenamiento(Manana, [e.C2014], lugar: "Coliseo")));
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(4)));

        Assert.True((await e.P.Cliente.PostAsync(e.Ruta($"/jugadores/{e.Jn(4).Id}/retiro"))).IsSuccessStatusCode);
        var retirado = await e.Jn(4).Cliente.GetAsync(e.Ruta("/campana"));
        await Actividades.LeerAsync(await e.P.Cliente.PostAsync(e.Ruta($"/jugadores/{e.Jn(4).Id}/reincorporacion")));
        await Actividades.LeerAsync(await e.P.Cliente.PutAsync(
            e.Ruta($"/jugadores/{e.Jn(4).Id}/categoria"), new { categoriaId = e.C2014.Id }));

        Assert.Equal(HttpStatusCode.Forbidden, retirado.StatusCode);
        Assert.Equal(1, await AyudaDeAvisos.SinLeerAsync(e, e.Jn(4)));
    }

    [Fact]
    public async Task Con_el_club_suspendido_las_familias_no_entran_y_ven_los_avisos_al_volver_a_la_normalidad()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync(
            e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])), HttpStatusCode.Created);
        await CambiarEstadoDelClubAsync(e, EstadoClub.SUSPENDIDO);

        var suspendido = await e.Jn(3).Cliente.GetAsync(e.Ruta("/campana"));
        await CambiarEstadoDelClubAsync(e, EstadoClub.ACTIVO);
        var activo = await e.Jn(3).Cliente.GetAsync(e.Ruta("/campana"));

        Assert.False(suspendido.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, activo.StatusCode);
        Assert.Single(await AyudaDeAvisos.TextosAsync(e, e.Jn(3)));
    }

    [Fact]
    public async Task Una_persona_de_dos_clubes_ve_los_avisos_y_el_contador_del_club_elegido()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        var cuenta = e.Jn(3).Cuenta;
        var otroClub = await _fabrica.Sembrador.CrearClubAsync();
        var enElOtro = await _fabrica.Sembrador.CrearIntegranteAsync(otroClub, cuenta, Rol.JUGADOR, nombres: "Tres");
        var categoriaDelOtro = await _fabrica.Categorias.CrearCategoriaAsync(otroClub, 2014);
        await _fabrica.Actividades.CambiarCategoriaAsync(enElOtro, categoriaDelOtro);
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync(
            e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])), HttpStatusCode.Created);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        var delClubA = await Actividades.LeerAsync(await cliente.GetAsync(e.Ruta("/campana/contador")));
        var delClubB = await Actividades.LeerAsync(await cliente.GetAsync($"/api/clubes/{otroClub.Id}/campana/contador"));

        Assert.Equal(1, delClubA.GetProperty("sinLeer").GetInt32());
        Assert.Equal(0, delClubB.GetProperty("sinLeer").GetInt32());
    }

    private Task<int> CambiarEstadoDelClubAsync(EscenarioActividades e, EstadoClub estado) =>
        _fabrica.ConContextoAsync(contexto => contexto.Clubes.Where(club => club.Id == e.Club.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(club => club.Estado, estado)));

    [Fact]
    public async Task El_desarrollador_y_el_equipo_del_club_no_tienen_avisos_ni_contador()
    {
        var e = await _fabrica.Actividades.CrearClubAsync();
        await _fabrica.Actividades.CrearEntrenamientoAsync([e.C2014], Manana);
        await Actividades.LeerAsync(await e.E1.Cliente.PostAsync(
            e.Ruta("/entrenamientos"), Actividades.Entrenamiento(Manana, [e.C2014])), HttpStatusCode.Created);

        var desarrollador = await _fabrica.CrearClienteDePrueba().ConSesionDeDesarrolladorAsync();

        await Actividades.ErrorAsync(
            await desarrollador.GetAsync(e.Ruta("/campana/contador")), HttpStatusCode.NotFound, "no_encontrado");
        foreach (var equipo in new[] { e.P, e.E1, e.E2, e.D1, e.D2 })
        {
            await Actividades.ErrorAsync(
                await equipo.Cliente.GetAsync(e.Ruta("/campana/contador")), HttpStatusCode.Forbidden, "rol_no_autorizado");
            Assert.Equal(0, await _fabrica.Avisos.ContarAsync(equipo.Cuenta));
        }
    }
}
