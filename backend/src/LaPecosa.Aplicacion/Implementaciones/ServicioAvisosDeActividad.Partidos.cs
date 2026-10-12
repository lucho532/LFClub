using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa la parte de <see cref="ServicioAvisosDeActividad"/> de los avisos de partido que
/// dependen de las personas: la convocatoria, un aviso por cuenta que nombra a sus jugadores y que
/// solo ellos reciben (RF-015; research §9), y el resultado, que llega a todas las categorías del
/// partido con el marcador en el orden en que lo ve la familia (RF-014).
/// No abre transacciones y no revela nombres de otros jugadores.
/// </summary>
public partial class ServicioAvisosDeActividad
{
    /// <inheritdoc />
    public async Task JugadorConvocadoAsync(
        Partido partido,
        IReadOnlyCollection<Guid> jugadoresConvocadosIds,
        Guid cuentaQueActua,
        CancellationToken cancelacion = default)
    {
        if (jugadoresConvocadosIds.Count == 0)
        {
            return;
        }

        var jugadores = await _avisos.JugadoresAsync(jugadoresConvocadosIds, cancelacion);
        var destinatarios = await _avisos.ResolverDestinatariosAsync(
            ReglaDestinatariosDeAviso.Evaluar(HechoDeActividad.JUGADOR_CONVOCADO, partido.Estado),
            [],
            partido.Id,
            jugadoresConvocadosIds,
            cuentaQueActua,
            cancelacion);
        var autorizados = destinatarios.Select(fila => fila.UsuarioRolId).ToHashSet();
        var porCuenta = jugadores.Where(jugador => autorizados.Contains(jugador.UsuarioRolId))
            .GroupBy(jugador => jugador.UsuarioId);

        // Un aviso por cuenta (research §9): nombra a sus jugadores convocados y solo ellos lo reciben.
        foreach (var deLaCuenta in porCuenta)
        {
            var elegidos = deLaCuenta.OrderBy(jugador => jugador.Nombres, StringComparer.CurrentCulture).ToList();
            var texto = RedaccionDeAvisos.JugadorConvocado(
                elegidos.Select(jugador => jugador.Nombres).ToList(), partido.Rival, partido.Fecha, partido.Hora, Hoy());
            await _avisos.GenerarAsync(
                NuevoAviso(partido.ClubId, HechoDeActividad.JUGADOR_CONVOCADO, TipoDeActividad.PARTIDO, partido.Id, texto),
                elegidos.Select(jugador => new DestinatarioDeAviso(jugador.UsuarioRolId, jugador.UsuarioId)).ToList(),
                cancelacion);
        }
    }

    /// <inheritdoc />
    public async Task ResultadoPublicadoAsync(Partido partido, Guid cuentaQueActua, CancellationToken cancelacion = default)
    {
        if (partido.GolesClub is not { } golesClub || partido.GolesRival is not { } golesRival)
        {
            return;
        }

        var club = await _club.ObtenerAsync(cancelacion);
        var texto = RedaccionDeAvisos.ResultadoPublicado(
            partido.Rival, partido.Condicion, club?.Nombre ?? string.Empty, golesClub, golesRival);
        await GenerarAsync(
            partido.ClubId,
            HechoDeActividad.RESULTADO_PUBLICADO,
            TipoDeActividad.PARTIDO,
            partido.Id,
            texto,
            ReglaDestinatariosDeAviso.Evaluar(HechoDeActividad.RESULTADO_PUBLICADO, partido.Estado),
            partido.Categorias.Select(fila => fila.CategoriaId).ToList(),
            partido.Id,
            cuentaQueActua,
            cancelacion);
    }
}
