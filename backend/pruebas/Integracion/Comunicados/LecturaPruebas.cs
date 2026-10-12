using System.Net;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Pruebas.Integracion.Comunicados;

/// <summary>
/// Leído y no leído: abrir un comunicado lo marca para la cuenta entera y "Marcar todos" los marca
/// todos (spec 011, historia 3; RF-020 a RF-023).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class LecturaPruebas
{
    private readonly FabricaApi _fabrica;

    public LecturaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    private async Task<List<Comunicado>> PublicarAsync(EscenarioComunicados e, int cuantos, params UsuarioRol[] destinatarios)
    {
        var lista = new List<Comunicado>();
        for (var i = 0; i < cuantos; i++)
        {
            lista.Add(await _fabrica.Comunicados.PublicarAsync(
                e.Club.Club, e.Club.Presidente.Integrante, $"Aviso {i}", "Texto", AlcanceComunicado.CLUB,
                destinatarios: destinatarios));
        }

        return lista;
    }

    [Fact]
    public async Task Leer_baja_el_contador_y_marcar_todos_lo_deja_en_cero_sin_tocar_a_las_otras_familias()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var comunicados = await PublicarAsync(e, 3, e.AnaJugador, e.Club.Caro.Integrante);
        Assert.Equal(3, await e.SinLeerAsync(e.Ana));

        var leido = await Actividades.LeerAsync(await e.Ana.PutAsync(e.Ruta($"/recibidos/{comunicados[0].Id}/lectura"), new { }));
        Assert.True(leido.GetProperty("leido").GetBoolean());
        Assert.True(leido.TryGetProperty("leidoEn", out _));
        Assert.Equal(2, await e.SinLeerAsync(e.Ana));

        var contador = await Actividades.LeerAsync(await e.Ana.PostAsync(e.RutaCampana("/lectura-total")));
        Assert.Equal(0, contador.GetProperty("sinLeer").GetInt32());
        Assert.Equal(0, await e.SinLeerAsync(e.Ana));
        // La otra familia sigue sin leer.
        Assert.Equal(3, await e.SinLeerAsync(e.Caro));
    }

    [Fact]
    public async Task Leerlo_otra_vez_no_cambia_la_fecha_y_persiste_en_otra_sesion()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var comunicado = (await PublicarAsync(e, 1, e.AnaJugador))[0];
        var ruta = e.Ruta($"/recibidos/{comunicado.Id}/lectura");

        var primera = await Actividades.LeerAsync(await e.Ana.PutAsync(ruta, new { }));
        var segunda = await Actividades.LeerAsync(await e.Ana.PutAsync(ruta, new { }));

        Assert.Equal(primera.GetProperty("leidoEn").GetDateTime(), segunda.GetProperty("leidoEn").GetDateTime());
        var otraSesion = (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(e.Club.Ana.Usuario))
            .ElegirJugador(e.AnaJugador.Id);
        var detalle = await Actividades.LeerAsync(await otraSesion.GetAsync(e.Ruta($"/recibidos/{comunicado.Id}")));
        Assert.True(detalle.GetProperty("leido").GetBoolean());
    }

    [Fact]
    public async Task Leerlo_desde_un_hermano_lo_deja_leido_para_toda_la_cuenta()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        var comunicado = (await PublicarAsync(e, 1, e.AnaJugador, hugo))[0];

        await Actividades.LeerAsync(await e.Ana.PutAsync(e.Ruta($"/recibidos/{comunicado.Id}/lectura"), new { }));

        var comoHugo = (await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(e.Club.Ana.Usuario)).ElegirJugador(hugo.Id);
        Assert.Equal(0, await e.SinLeerAsync(comoHugo));
        var filas = await _fabrica.ConContextoAsync(contexto => contexto.ComunicadosDestinatario
            .IgnoreQueryFilters().Where(fila => fila.ComunicadoId == comunicado.Id).ToListAsync());
        Assert.Equal(2, filas.Count);
        Assert.Single(filas.Select(fila => fila.LeidoEn).Distinct());
        Assert.NotNull(filas[0].LeidoEn);
    }

    [Fact]
    public async Task Una_sesion_limitada_a_un_jugador_marca_leido_para_la_cuenta_entera()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var hugo = await _fabrica.Hermanos.CrearHermanoAsync(
            e.AnaJugador, EstadoIngreso.APROBADO, categoria: e.Club.C2015, nombres: "Hugo");
        var comunicado = (await PublicarAsync(e, 1, e.AnaJugador, hugo))[0];
        var limitada = await _fabrica.CrearClienteDePrueba().ConSesionLimitadaAsync(e.Club.Ana.Usuario, e.AnaJugador.Id);

        await Actividades.LeerAsync(await limitada.PutAsync(e.Ruta($"/recibidos/{comunicado.Id}/lectura"), new { }));

        var filas = await _fabrica.ConContextoAsync(contexto => contexto.ComunicadosDestinatario
            .IgnoreQueryFilters().Where(fila => fila.ComunicadoId == comunicado.Id).ToListAsync());
        Assert.All(filas, fila => Assert.NotNull(fila.LeidoEn));
    }

    [Fact]
    public async Task Lo_ajeno_lo_retirado_y_lo_de_otro_club_responden_404_y_no_marcan_nada()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);
        var otro = await EscenarioComunicados.CrearAsync(_fabrica);
        var deCaro = (await PublicarAsync(e, 1, e.Club.Caro.Integrante))[0];
        var retirado = (await PublicarAsync(e, 1, e.AnaJugador))[0];
        await _fabrica.Comunicados.RetirarAsync(retirado, e.Club.Presidente.Integrante);

        await Actividades.ErrorAsync(
            await e.Ana.PutAsync(e.Ruta($"/recibidos/{deCaro.Id}/lectura"), new { }), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await e.Ana.PutAsync(e.Ruta($"/recibidos/{retirado.Id}/lectura"), new { }), HttpStatusCode.NotFound, "no_encontrado");
        await Actividades.ErrorAsync(
            await otro.Ana.PutAsync(e.Ruta($"/recibidos/{deCaro.Id}/lectura"), new { }), HttpStatusCode.NotFound, "no_encontrado");

        Assert.Equal(1, await e.SinLeerAsync(e.Caro));
    }

    [Fact]
    public async Task Los_roles_del_equipo_reciben_403_al_marcar_la_lectura()
    {
        var e = await EscenarioComunicados.CrearAsync(_fabrica);

        await Actividades.ErrorAsync(
            await e.P.PostAsync(e.RutaCampana("/lectura-total")), HttpStatusCode.Forbidden, "rol_no_autorizado");
        await Actividades.ErrorAsync(
            await e.E1.PutAsync(e.Ruta($"/recibidos/{Guid.NewGuid()}/lectura"), new { }),
            HttpStatusCode.Forbidden,
            "rol_no_autorizado");
    }
}
