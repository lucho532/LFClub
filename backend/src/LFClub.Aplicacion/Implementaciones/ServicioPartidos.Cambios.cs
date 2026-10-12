using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa la parte de <see cref="ServicioPartidos"/> que cambia un partido que ya existe: sus
/// datos, su estado al cancelarlo y al reactivarlo (spec 009, RF-008, RF-014 y RF-021; spec 012,
/// RF-001, RF-004 y RF-007).
/// Su responsabilidad es tomar el bloqueo del club, volver a leer el partido dentro de la
/// transacción, aplicar el cambio y avisar a las familias en esa misma transacción solo si cambió
/// algo de verdad: fecha, hora o lugar, o el estado. Cambiar el rival, la condición o el torneo no
/// avisa. Un partido próximo avisa solo a sus convocados; uno jugado, a sus categorías.
/// No toca la convocatoria ni el resultado y no decide quién lo ve.
/// </summary>
public partial class ServicioPartidos
{
    /// <inheritdoc />
    public async Task<PartidoDto> CambiarAsync(
        Guid partidoId, GuardarPartidoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var validos = ValidadorPartido.Validar(datos);
        await GestionadoAsync(partidoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var antes = await _partidos.ObtenerAsync(partidoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (!ReglaTransicionPartido.PuedeCambiarDatos(antes.Estado))
                {
                    throw ErrorDeEstado(antes.Estado);
                }

                if (!antes.Categorias.Select(fila => fila.CategoriaId).ToHashSet().SetEquals(validos.CategoriaIds))
                {
                    throw ErroresDeActividades.CategoriasNoCambian();
                }

                var cambios = ReglaCambiosDeActividad.Evaluar(
                    antes.Fecha, antes.Hora, null, antes.Lugar, validos.Fecha, validos.Hora, null, validos.Lugar);
                await _partidos.CambiarDatosAsync(partidoId, Con(validos, new Partido()), cancelacion);

                if (cambios != CambiosDeActividad.Ninguno)
                {
                    var despues = Con(
                        validos,
                        new Partido { Id = antes.Id, ClubId = antes.ClubId, Estado = antes.Estado, Categorias = antes.Categorias });
                    await _avisos.PartidoCambiadoAsync(despues, cambios, quienPregunta.UsuarioId, cancelacion);
                }
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PartidoDto> CancelarAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await GestionadoAsync(partidoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var partido = await _partidos.ObtenerAsync(partidoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (!ReglaTransicionPartido.PuedeCancelar(partido.Estado))
                {
                    throw ErrorDeEstado(partido.Estado);
                }

                await _partidos.CambiarEstadoAsync(partidoId, EstadoPartido.CANCELADO, cancelacion);
                await _avisos.PartidoCanceladoAsync(partido, quienPregunta.UsuarioId, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PartidoDto> ReactivarAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        await GestionadoAsync(partidoId, quienPregunta, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var partido = await _partidos.ObtenerAsync(partidoId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                if (!ReglaTransicionPartido.PuedeReactivar(partido.Estado))
                {
                    throw ErroresDeActividades.ActividadNoCancelada();
                }

                var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
                if (ReglaMomentoDeActividad.YaEmpezo(partido.Fecha, partido.Hora, hoy, ahora))
                {
                    throw ErroresDeActividades.ActividadEmpezada();
                }

                await ExigirActivasAsync(partido.Categorias.Select(fila => fila.CategoriaId).ToList(), cancelacion);
                await _partidos.CambiarEstadoAsync(partidoId, EstadoPartido.PROXIMO, cancelacion);
                await _avisos.PartidoReactivadoAsync(partido, quienPregunta.UsuarioId, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(partidoId, quienPregunta, cancelacion);
    }
}
