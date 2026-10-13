using System.Net;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Cuenta.FamiliaParaEliminar;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Eliminar la propia cuenta con la contraseña, y lo que desaparece con ella: sesión, credenciales,
/// jugadores en todos los clubes y su historia deportiva, con el marcador intacto (spec 014,
/// historias 1 y 2; RF-002 a RF-006, RF-010, RF-011 y RF-013).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EliminarCuentaPruebas
{
    private readonly FabricaApi _fabrica;

    public EliminarCuentaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private static object Cuerpo(string contrasena = Sembrador.Contrasena) => new { contrasena };

    [Fact]
    public async Task Con_la_contrasena_correcta_la_cuenta_desaparece_y_responde_204()
    {
        var f = await CrearAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        var respuesta = await cliente.PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.False(await ExisteCuentaAsync(_fabrica, f.Cuenta.Id));
        foreach (var jugador in f.Jugadores)
        {
            Assert.False(await ExisteIntegranteAsync(_fabrica, jugador.Id));
        }
    }

    [Fact]
    public async Task Una_contrasena_incorrecta_responde_400_y_no_borra_nada_pero_suma_un_fallo()
    {
        var f = await CrearAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        var respuesta = await cliente.PostAsync(Ruta, Cuerpo("incorrecta"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("contrasena_incorrecta", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True(await ExisteCuentaAsync(_fabrica, f.Cuenta.Id));
        Assert.True(await ExisteIntegranteAsync(_fabrica, f.JugadorA1.Id));
        var cuenta = await _fabrica.ConContextoAsync(c => c.Usuarios.SingleAsync(u => u.Id == f.Cuenta.Id));
        Assert.Equal(1, cuenta.IntentosFallidos);
    }

    [Fact]
    public async Task Sin_contrasena_responde_400_datos_invalidos()
    {
        var f = await CrearAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new { });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("datos_invalidos", await ClienteDePrueba.CodigoAsync(respuesta));
        Assert.True(await ExisteCuentaAsync(_fabrica, f.Cuenta.Id));
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_la_cuenta_y_ni_la_contrasena_correcta_la_elimina()
    {
        var f = await CrearAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        for (var intento = 1; intento <= 5; intento++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync(Ruta, Cuerpo("incorrecta"))).StatusCode);
        }

        var conLaCorrecta = await cliente.PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Forbidden, conLaCorrecta.StatusCode);
        Assert.Equal("cuenta_bloqueada", await ClienteDePrueba.CodigoAsync(conLaCorrecta));
        Assert.True(await ExisteCuentaAsync(_fabrica, f.Cuenta.Id));
    }

    [Fact]
    public async Task Otra_sesion_de_la_cuenta_recibe_401_en_su_siguiente_peticion()
    {
        var f = await CrearAsync(_fabrica);
        var otraSesion = await f.ClienteAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        await cliente.PostAsync(Ruta, Cuerpo());
        var siguiente = await otraSesion.GetAsync("/api/sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, siguiente.StatusCode);
    }

    [Fact]
    public async Task Entrar_con_el_correo_o_con_el_documento_responde_lo_mismo_que_una_credencial_desconocida()
    {
        var f = await CrearAsync(_fabrica);
        var documento = f.JugadorA1.NumeroDocumento;
        await (await f.ClienteAsync(_fabrica)).PostAsync(Ruta, Cuerpo());
        var anonimo = _fabrica.CrearClienteDePrueba();

        var porCorreo = await anonimo.IniciarSesionAsync(f.Cuenta.Correo);
        var porDocumento = await anonimo.IniciarSesionAsync(documento);
        var desconocida = await anonimo.IniciarSesionAsync(Sembrador.CorreoUnico());

        foreach (var respuesta in new[] { porCorreo, porDocumento, desconocida })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal("credenciales_invalidas", await ClienteDePrueba.CodigoAsync(respuesta));
        }

        Assert.Equal(await ClienteDePrueba.TituloAsync(desconocida), await ClienteDePrueba.TituloAsync(porCorreo));
    }

    [Fact]
    public async Task El_correo_y_los_documentos_quedan_libres()
    {
        var f = await CrearAsync(_fabrica);
        var documentos = f.Jugadores.Select(jugador => jugador.NumeroDocumento).ToList();
        await (await f.ClienteAsync(_fabrica)).PostAsync(Ruta, Cuerpo());

        var correoOcupado = await _fabrica.ConContextoAsync(c =>
            c.Usuarios.AnyAsync(u => u.CorreoNormalizado == f.Cuenta.CorreoNormalizado));
        var documentosOcupados = await _fabrica.ConContextoAsync(c => c.UsuariosRol.IgnoreQueryFilters()
            .CountAsync(i => documentos.Contains(i.NumeroDocumento)));

        Assert.False(correoOcupado);
        Assert.Equal(0, documentosOcupados);
    }

    [Fact]
    public async Task Otra_cuenta_y_otros_clubes_quedan_intactos()
    {
        var f = await CrearAsync(_fabrica);
        var (otraCuenta, otroJugador) = await _fabrica.Categorias.CrearJugadorAsync(f.ClubA, 2014, f.CategoriaA);

        await (await f.ClienteAsync(_fabrica)).PostAsync(Ruta, Cuerpo());

        Assert.True(await ExisteCuentaAsync(_fabrica, otraCuenta.Id));
        Assert.True(await ExisteIntegranteAsync(_fabrica, otroJugador.Id));
        Assert.True(await ExisteIntegranteAsync(_fabrica, f.PresidenteA.Id));
        Assert.True(await ExisteIntegranteAsync(_fabrica, f.PresidenteB.Id));
        Assert.True(await _fabrica.ConContextoAsync(c => c.Clubes.AnyAsync(club => club.Id == f.ClubB.Id)));
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await _fabrica.CrearClienteDePrueba().PostAsync(Ruta, Cuerpo());

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_resumen_lista_a_los_tres_jugadores_con_su_club_y_estado_sin_documento()
    {
        var f = await CrearAsync(_fabrica);
        var cliente = await f.ClienteAsync(_fabrica);

        var texto = await (await cliente.GetAsync(Ruta)).Content.ReadAsStringAsync();
        var resumen = System.Text.Json.JsonDocument.Parse(texto).RootElement;

        Assert.True(resumen.GetProperty("puedeEliminar").GetBoolean());
        var jugadores = resumen.GetProperty("jugadores").EnumerateArray().ToList();
        Assert.Equal(3, jugadores.Count);
        var sara = jugadores.Single(j => j.GetProperty("nombreCompleto").GetString()!.StartsWith("Sara"));
        Assert.Equal(f.ClubB.Nombre, sara.GetProperty("clubNombre").GetString());
        Assert.True(sara.GetProperty("enEspera").GetBoolean());
        var mateo = jugadores.Single(j => j.GetProperty("nombreCompleto").GetString()!.StartsWith("Mateo"));
        Assert.True(mateo.GetProperty("retirado").GetBoolean());
        Assert.Equal(f.ClubA.Nombre, mateo.GetProperty("clubNombre").GetString());

        Assert.All(f.Jugadores, jugador => Assert.DoesNotContain(jugador.NumeroDocumento, texto));
    }

    [Fact]
    public async Task Una_sesion_limitada_por_documento_ve_y_elimina_a_todos_los_jugadores_de_la_cuenta()
    {
        var f = await CrearAsync(_fabrica);
        var limitada = await _fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(f.Cuenta, f.JugadorA1.Id);

        var resumen = await CuerpoAsync(await limitada.GetAsync(Ruta));
        var respuesta = await limitada.PostAsync(Ruta, Cuerpo());

        Assert.Equal(3, resumen.GetProperty("jugadores").GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.False(await ExisteIntegranteAsync(_fabrica, f.JugadorBEnEspera.Id));
    }

    [Fact]
    public async Task Tras_eliminar_desaparecen_convocatoria_goles_asistencia_ficha_y_carta_y_el_marcador_no_cambia()
    {
        var f = await CrearAsync(_fabrica);
        var otro = await _fabrica.Categorias.CrearJugadorAsync(f.ClubA, 2014, f.CategoriaA, nombres: "Otro");
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [f.CategoriaA], new DateOnly(2026, 8, 1), convocados: [f.JugadorA1, otro.Integrante]);
        await _fabrica.Actividades.RegistrarJugadoAsync(partido, 3, 1, (f.JugadorA1, 2), (otro.Integrante, 1));
        var entrenamiento = await _fabrica.Actividades.CrearEntrenamientoAsync([f.CategoriaA], new DateOnly(2026, 8, 2));
        await _fabrica.Actividades.RegistrarAsistenciaAsync(entrenamiento, f.JugadorA1, true);
        await _fabrica.Actividades.RegistrarAsistenciaAsync(entrenamiento, otro.Integrante, true);
        await _fabrica.Fichas.CrearFichaAsync(f.JugadorA1);
        await _fabrica.Cartas.PonerCartaAsync(f.JugadorA1, dorsal: 9, estadisticas: [50, 50, 50, 50, 50, 50]);

        await (await f.ClienteAsync(_fabrica)).PostAsync(Ruta, Cuerpo());

        await _fabrica.ConContextoAsync(async contexto =>
        {
            var ids = f.Jugadores.Select(j => j.Id).ToList();
            var guardado = await contexto.Partidos.IgnoreQueryFilters().SingleAsync(p => p.Id == partido.Id);
            Assert.Equal(3, guardado.GolesClub);
            Assert.Equal(1, guardado.GolesRival);
            var filas = await contexto.PartidosJugador.IgnoreQueryFilters().Where(p => p.PartidoId == partido.Id).ToListAsync();
            Assert.Single(filas);
            Assert.Equal(otro.Integrante.Id, filas[0].UsuarioRolId);
            Assert.Single(await contexto.AsistenciasEntrenamiento.IgnoreQueryFilters()
                .Where(a => a.EntrenamientoId == entrenamiento.Id).ToListAsync());
            Assert.False(await contexto.FichasJugador.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.UsuarioRolId)));
            Assert.False(await contexto.CartasJugador.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.UsuarioRolId)));
            return 0;
        });
    }
}
