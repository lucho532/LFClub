using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;
using LFClub.Pruebas.Integracion.Base;
using Microsoft.EntityFrameworkCore;
using static LFClub.Pruebas.Integracion.Cuenta.FamiliaParaEliminar;

namespace LFClub.Pruebas.Integracion.Cuenta;

/// <summary>
/// Lo que una persona publicó se conserva con "Persona eliminada" como autor, y sus invitaciones
/// pendientes dejan de servir (spec 014, historia 3; RF-008 y RF-012; constitución §13).
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class AutoriaAnonimaPruebas
{
    private readonly FabricaApi _fabrica;

    public AutoriaAnonimaPruebas(FabricaApi fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Lo_publicado_se_conserva_con_persona_eliminada_y_ningun_nombre_propio_sobrevive()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var categoria = await _fabrica.Categorias.CrearCategoriaAsync(club, 2014);
        var (cuenta, directivo) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.DIRECTIVO);
        var (cuentaJugador, jugador) = await _fabrica.Categorias.CrearJugadorAsync(club, 2014, categoria);
        var nombre = $"{directivo.Nombres} {directivo.Apellidos}";

        var comunicado = await _fabrica.Comunicados.PublicarAsync(
            club, directivo, "Aviso", "Texto", AlcanceComunicado.CLUB, destinatarios: [jugador]);
        await _fabrica.Comunicados.RetirarAsync(comunicado, directivo);
        var partido = await _fabrica.Actividades.CrearPartidoAsync(
            [categoria], new DateOnly(2026, 8, 1), convocados: [jugador]);
        await _fabrica.Actividades.RegistrarJugadoAsync(partido, 2, 0, (jugador, 1));
        await _fabrica.Fichas.CrearFichaAsync(jugador, cambiadaPor: directivo);
        await _fabrica.Cartas.PonerCartaAsync(jugador, dorsal: 7, estadisticas: [60, 60, 60, 60, 60, 60]);
        await _fabrica.ConContextoAsync(async contexto =>
        {
            await contexto.Partidos.IgnoreQueryFilters().Where(p => p.Id == partido.Id).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PublicadoPorUsuarioId, cuenta.Id).SetProperty(p => p.PublicadoPorNombre, nombre)
                .SetProperty(p => p.ResultadoCambiadoPorUsuarioId, cuenta.Id)
                .SetProperty(p => p.ResultadoCambiadoPorNombre, nombre));
            await contexto.PartidosJugador.IgnoreQueryFilters().Where(f => f.PartidoId == partido.Id).ExecuteUpdateAsync(s => s
                .SetProperty(f => f.IndicadoPor, OrigenResultado.GESTION).SetProperty(f => f.IndicadoEn, DateTime.UtcNow)
                .SetProperty(f => f.IndicadoPorUsuarioId, cuenta.Id).SetProperty(f => f.IndicadoPorNombre, nombre));
            await contexto.CartasJugador.IgnoreQueryFilters().Where(c => c.UsuarioRolId == jugador.Id).ExecuteUpdateAsync(s => s
                .SetProperty(c => c.EstadisticasCambiadasPorUsuarioId, cuenta.Id)
                .SetProperty(c => c.EstadisticasCambiadasPorNombre, nombre)
                .SetProperty(c => c.DorsalCambiadoPorUsuarioId, cuenta.Id)
                .SetProperty(c => c.DorsalCambiadoPorNombre, nombre));
            await contexto.UsuariosRol.IgnoreQueryFilters().Where(i => i.Id == jugador.Id).ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AprobadoPorUsuarioId, cuenta.Id).SetProperty(i => i.AprobadoPorNombre, nombre)
                .SetProperty(i => i.RetiradoPorUsuarioId, cuenta.Id).SetProperty(i => i.RetiradoPorNombre, nombre));
            return 0;
        });
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        await cliente.PostAsync(Ruta, new { contrasena = Sembrador.Contrasena });

        Assert.False(await ExisteCuentaAsync(_fabrica, cuenta.Id));
        Assert.True(await ExisteCuentaAsync(_fabrica, cuentaJugador.Id));
        await _fabrica.ConContextoAsync(async contexto =>
        {
            const string anonimo = NombresDeAutoria.PersonaEliminada;
            var publicado = await contexto.Comunicados.IgnoreQueryFilters().SingleAsync(c => c.Id == comunicado.Id);
            Assert.Equal(anonimo, publicado.PublicadoPorNombre);
            Assert.Equal(anonimo, publicado.RetiradoPorNombre);
            Assert.Null(publicado.PublicadoPorUsuarioRolId);
            var guardado = await contexto.Partidos.IgnoreQueryFilters().SingleAsync(p => p.Id == partido.Id);
            Assert.Equal(anonimo, guardado.PublicadoPorNombre);
            Assert.Equal(anonimo, guardado.ResultadoCambiadoPorNombre);
            var fila = await contexto.PartidosJugador.IgnoreQueryFilters().SingleAsync(f => f.PartidoId == partido.Id);
            Assert.Equal(anonimo, fila.IndicadoPorNombre);
            var carta = await contexto.CartasJugador.IgnoreQueryFilters().SingleAsync(c => c.UsuarioRolId == jugador.Id);
            Assert.Equal(anonimo, carta.EstadisticasCambiadasPorNombre);
            Assert.Equal(anonimo, carta.DorsalCambiadoPorNombre);
            var ficha = await contexto.FichasJugador.IgnoreQueryFilters().SingleAsync(f => f.UsuarioRolId == jugador.Id);
            Assert.Equal(anonimo, ficha.UltimoCambioPorNombre);
            var integrante = await contexto.UsuariosRol.IgnoreQueryFilters().SingleAsync(i => i.Id == jugador.Id);
            Assert.Equal(anonimo, integrante.AprobadoPorNombre);
            Assert.Equal(anonimo, integrante.RetiradoPorNombre);
            return 0;
        });
    }

    [Fact]
    public async Task Las_invitaciones_pendientes_quedan_anuladas_y_las_usadas_no_cambian()
    {
        var club = await _fabrica.Sembrador.CrearClubAsync();
        await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var (cuenta, _) = await _fabrica.Sembrador.CrearIntegranteAsync(club, Rol.PRESIDENTE);
        var usada = DateTime.UtcNow.AddDays(-1);
        var (pendiente, _) = await _fabrica.Sembrador.CrearInvitacionAsync(club, creadaPor: cuenta, rol: Rol.JUGADOR);
        var (yaUsada, _) = await _fabrica.Sembrador.CrearInvitacionAsync(
            club, creadaPor: cuenta, rol: Rol.JUGADOR, usadaEn: usada);
        var cliente = await _fabrica.CrearClienteDePrueba().ConSesionDeAsync(cuenta);

        await cliente.PostAsync(Ruta, new { contrasena = Sembrador.Contrasena });

        var (despuesPendiente, despuesUsada) = await _fabrica.ConContextoAsync(async contexto => (
            await contexto.Invitaciones.IgnoreQueryFilters().SingleAsync(i => i.Id == pendiente.Id),
            await contexto.Invitaciones.IgnoreQueryFilters().SingleAsync(i => i.Id == yaUsada.Id)));
        Assert.NotNull(despuesPendiente.AnuladaEn);
        Assert.Null(despuesUsada.AnuladaEn);
        Assert.NotNull(despuesUsada.UsadaEn);
    }
}
