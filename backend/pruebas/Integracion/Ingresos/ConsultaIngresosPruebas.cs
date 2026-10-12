using System.Net;
using System.Text.Json;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Pruebas.Integracion.Ingresos;

/// <summary>
/// La sala de espera del club y su lista de ingresos aprobados, que solo ve el PRESIDENTE (RF-017
/// y RF-019). Acompaña a <see cref="AprobacionPruebas"/>.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class ConsultaIngresosPruebas
{
    private readonly FabricaApi _fabrica;

    public ConsultaIngresosPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task La_sala_de_espera_muestra_a_todos_los_que_esperan_con_sus_datos_del_mas_antiguo_al_mas_reciente()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var cliente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.JUGADOR);
        var (primera, suIngreso) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, segundo) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        await _fabrica.ConContextoAsync(contexto => contexto.Usuarios.Where(u => u.Id == primera.Id)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(u => u.NombreResponsable, "Marta Gómez")));
        // En otro club también hay alguien esperando: no debe aparecer aquí.
        await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(await _fabrica.Sembrador.CrearClubAsync());

        var filas = await cliente.ListaAsync(EscenarioIngresos.EnEspera(club));

        Assert.Equal([suIngreso.Id, segundo.Id], filas.Select(fila => fila.GetProperty("usuarioRolId").GetGuid()).ToList());
        var fila = filas[0];
        Assert.Equal(suIngreso.Nombres, fila.GetProperty("nombres").GetString());
        Assert.Equal(suIngreso.Apellidos, fila.GetProperty("apellidos").GetString());
        Assert.Equal("CEDULA_CIUDADANIA", fila.GetProperty("tipoDocumento").GetString());
        Assert.Equal(suIngreso.NumeroDocumento, fila.GetProperty("numeroDocumento").GetString());
        Assert.Equal("1990-05-20", fila.GetProperty("fechaNacimiento").GetString());
        Assert.Equal(primera.Correo, fila.GetProperty("correo").GetString());
        Assert.Equal("3001234567", fila.GetProperty("celular").GetString());
        Assert.Equal("Marta Gómez", fila.GetProperty("nombreResponsable").GetString());
        Assert.True(fila.GetProperty("registradoEn").GetDateTime() > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(JsonValueKind.Null, filas[1].GetProperty("nombreResponsable").ValueKind);
    }

    [Fact]
    public async Task Nadie_mas_ve_la_sala_de_espera_ni_la_lista_de_aprobados()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (enEspera, _) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var deQuienEspera = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(enEspera);
        var casos = new (ClienteDePrueba Cliente, HttpStatusCode Estado, string Codigo)[]
        {
            (await _fabrica.ClienteDeAsync(club, Rol.DIRECTIVO), HttpStatusCode.Forbidden, "rol_no_autorizado"),
            (await _fabrica.ClienteDeAsync(club, Rol.ENTRENADOR), HttpStatusCode.Forbidden, "rol_no_autorizado"),
            (await _fabrica.ClienteDeAsync(club, Rol.JUGADOR), HttpStatusCode.Forbidden, "rol_no_autorizado"),
            (deQuienEspera, HttpStatusCode.Forbidden, "ingreso_en_espera"),
            (_fabrica.CrearClienteDePrueba(), HttpStatusCode.Unauthorized, "sin_sesion"),
        };

        foreach (var (cliente, estado, codigo) in casos)
        {
            foreach (var ruta in new[] { EscenarioIngresos.EnEspera(club), EscenarioIngresos.Aprobados(club) })
            {
                var respuesta = await cliente.GetAsync(ruta);

                Assert.Equal(estado, respuesta.StatusCode);
                Assert.Equal(codigo, await ClienteDePrueba.CodigoAsync(respuesta));
                Assert.DoesNotContain(enEspera.Correo, await respuesta.Content.ReadAsStringAsync());
            }
        }
    }

    [Fact]
    public async Task La_lista_de_aprobados_dice_quien_aprobo_y_cuando_y_no_incluye_a_quien_entro_con_una_invitacion()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var (presidente, integrantePresidente) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(presidente);
        var (_, primero) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        var (_, segundo) = await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        await _fabrica.Sembrador.CrearIntegranteEnEsperaAsync(club);
        Assert.Empty(await cliente.ListaAsync(EscenarioIngresos.Aprobados(club)));

        await cliente.PostAsync(EscenarioIngresos.Aprobacion(club, primero.Id), new { rol = "JUGADOR" });
        await cliente.PostAsync(EscenarioIngresos.Aprobacion(club, segundo.Id));
        // Quien entra con una invitación no genera una aprobación (RF-019).
        var (_, conInvitacion) = await _fabrica.RegistrarConInvitacionAsync(club, Rol.ENTRENADOR);

        // Ni el presidente, ni quien entró con una invitación del club, ni quien sigue en espera.
        var filas = await cliente.ListaAsync(EscenarioIngresos.Aprobados(club));
        Assert.Equal([segundo.Id, primero.Id], filas.Select(fila => fila.GetProperty("usuarioRolId").GetGuid()).ToList());
        Assert.All(filas, fila => Assert.Equal("JUGADOR", fila.GetProperty("rolDeIngreso").GetString()));
        Assert.DoesNotContain(conInvitacion.Id, filas.Select(fila => fila.GetProperty("usuarioRolId").GetGuid()));
        Assert.Equal(segundo.Nombres, filas[0].GetProperty("nombres").GetString());
        Assert.Equal(segundo.Apellidos, filas[0].GetProperty("apellidos").GetString());
        Assert.Equal(
            $"{integrantePresidente.Nombres} {integrantePresidente.Apellidos}", filas[0].GetProperty("aprobadoPor").GetString());
        Assert.True(filas[0].GetProperty("aprobadoEn").GetDateTime() >= filas[1].GetProperty("aprobadoEn").GetDateTime());
        Assert.DoesNotContain(integrantePresidente.Id, filas.Select(fila => fila.GetProperty("usuarioRolId").GetGuid()));
    }

    [Theory]
    [InlineData(Rol.ENTRENADOR)]
    [InlineData(Rol.DIRECTIVO)]
    public async Task Las_aprobaciones_anteriores_conservan_su_rol_de_ingreso_y_quien_aprobo_aunque_fuera_un_directivo(Rol rolDeIngreso)
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        var presidente = await _fabrica.ClienteDeAsync(club, Rol.PRESIDENTE);
        var (directivo, integranteDirectivo) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var nombreDelDirectivo = $"{integranteDirectivo.Nombres} {integranteDirectivo.Apellidos}";

        // Una aprobación de antes del cambio: la hizo un directivo y eligió otro rol. Ya no hay
        // forma de producirla por la API, así que se siembra tal como quedó guardada.
        var (_, anterior) = await _fabrica.Sembrador.CrearIntegranteAsync(club, rolDeIngreso);
        var aprobadoEn = DateTime.UtcNow.AddDays(-30);
        await _fabrica.ConContextoAsync(contexto => IntegrantesDe(contexto.UsuariosRol, anterior.Id)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(i => i.RolDeIngreso, rolDeIngreso)
                .SetProperty(i => i.AprobadoEn, aprobadoEn)
                .SetProperty(i => i.AprobadoPorUsuarioId, directivo.Id)
                .SetProperty(i => i.AprobadoPorNombre, nombreDelDirectivo)));

        // Después cambia el rol actual de la persona y se elimina la cuenta de quien aprobó (§13).
        await _fabrica.ConContextoAsync(async contexto =>
        {
            await IntegrantesDe(contexto.UsuariosRol, anterior.Id)
                .ExecuteUpdateAsync(cambios => cambios.SetProperty(i => i.Rol, Rol.JUGADOR));
            await IntegrantesDe(contexto.UsuariosRol, integranteDirectivo.Id).ExecuteDeleteAsync();
            return await contexto.Usuarios.Where(u => u.Id == directivo.Id).ExecuteDeleteAsync();
        });

        var fila = Assert.Single(await presidente.ListaAsync(EscenarioIngresos.Aprobados(club)));
        Assert.Equal(anterior.Id, fila.GetProperty("usuarioRolId").GetGuid());
        Assert.Equal(rolDeIngreso.ToString(), fila.GetProperty("rolDeIngreso").GetString());
        Assert.Equal(nombreDelDirectivo, fila.GetProperty("aprobadoPor").GetString());
        Assert.Equal(aprobadoEn, fila.GetProperty("aprobadoEn").GetDateTime(), TimeSpan.FromMilliseconds(1));
        Assert.Null((await _fabrica.IntegranteAsync(anterior.Id))!.AprobadoPorUsuarioId);
    }

    private static IQueryable<UsuarioRol> IntegrantesDe(DbSet<UsuarioRol> integrantes, Guid usuarioRolId) =>
        integrantes.IgnoreQueryFilters().Where(integrante => integrante.Id == usuarioRolId);
}
