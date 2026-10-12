using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la planilla y la convocatoria de un partido.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAActividad"/>; dar la planilla solo
/// a quien puede verla (a los demás que ven el partido, <c>403 no_gestiona_actividad</c>); y guardar
/// la convocatoria solo en un partido próximo que quien pregunta gestiona, con jugadores activos de
/// sus categorías (RF-009). Quien sigue convocado conserva su fila, su categoría copiada y su
/// respuesta aunque haya cambiado de categoría (RF-034); quien sale pierde su fila y su respuesta
/// (RF-013). Al guardar toma el bloqueo del club, vuelve a leer el partido y, en la misma
/// transacción, avisa a la familia de cada jugador recién convocado, con un solo aviso por cuenta
/// (spec 012, RF-002 y RF-015). Sacar a alguien de la convocatoria no avisa a nadie.
/// No decide quién ve el partido y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioConvocatoria : IServicioConvocatoria
{
    private readonly AccesoAActividad _acceso;
    private readonly IRepositorioConvocatoria _convocatoria;
    private readonly IRepositorioClub _club;
    private readonly IServicioAvisosDeActividad _avisos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConvocatoria(
        AccesoAActividad acceso,
        IRepositorioConvocatoria convocatoria,
        IRepositorioClub club,
        IServicioAvisosDeActividad avisos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _acceso = acceso;
        _convocatoria = convocatoria;
        _club = club;
        _avisos = avisos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<PlanillaPartidoDto> PlanillaAsync(
        Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        if (!visible.Permisos.VePlanilla)
        {
            throw ErroresDeActividades.NoGestionaActividad();
        }

        return await LeerAsync(visible, cancelacion);
    }

    /// <inheritdoc />
    public async Task<PlanillaPartidoDto> GuardarAsync(
        Guid partidoId, GuardarConvocatoriaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var pedidos = Validar(datos);
        var visible = await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion);
        AccesoAActividad.ExigirGestion(visible.Permisos);
        if (!ReglaTransicionPartido.PuedeConvocar(visible.Partido.Estado))
        {
            throw ServicioPartidos.ErrorDeEstado(visible.Partido.Estado);
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Con el club bloqueado se vuelve a leer el partido: un cambio de estado a la vez no
                // deja convocar a un partido cancelado ni jugado (spec 012, research §5).
                await _club.BloquearAsync(cancelacion);
                var vigente = (await _acceso.PartidoAsync(partidoId, quienPregunta, cancelacion)).Partido;
                if (!ReglaTransicionPartido.PuedeConvocar(vigente.Estado))
                {
                    throw ServicioPartidos.ErrorDeEstado(vigente.Estado);
                }

                var actuales = (await _convocatoria.ConvocadosAsync(partidoId, cancelacion))
                    .Select(fila => fila.UsuarioRolId)
                    .ToHashSet();
                var nuevos = pedidos.Where(id => !actuales.Contains(id)).ToList();
                var categorias = visible.Partido.Categorias.Select(fila => fila.CategoriaId).ToHashSet();
                var integrantes = await _convocatoria.IntegrantesAsync(nuevos, cancelacion);
                var validos = integrantes
                    .Where(jugador => jugador.EsJugadorDelClub
                        && jugador.CategoriaId is { } categoria
                        && categorias.Contains(categoria))
                    .ToList();
                if (validos.Count != nuevos.Count)
                {
                    throw ErroresDeActividades.JugadorFueraDeLaActividad();
                }

                var bajas = actuales.Where(id => !pedidos.Contains(id)).ToList();
                await _convocatoria.ReemplazarAsync(partidoId, validos, bajas, _reloj.AhoraUtc, cancelacion);
                await _avisos.JugadorConvocadoAsync(
                    vigente, validos.Select(jugador => jugador.Id).ToList(), quienPregunta.UsuarioId, cancelacion);
            },
            cancelacion);

        return await LeerAsync(visible, cancelacion);
    }

    private async Task<PlanillaPartidoDto> LeerAsync(PartidoVisible visible, CancellationToken cancelacion)
    {
        var partido = visible.Partido;
        var convocados = await _convocatoria.ConvocadosAsync(partido.Id, cancelacion);
        IReadOnlyList<UsuarioRol> convocables = partido.Estado == EstadoPartido.PROXIMO
            ? await _convocatoria.ConvocablesAsync(
                partido.Id, partido.Categorias.Select(fila => fila.CategoriaId).ToList(), cancelacion)
            : [];
        return MapperPartidos.APlanilla(partido.Id, convocados, convocables, visible.Quien);
    }

    private static HashSet<Guid> Validar(GuardarConvocatoriaDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.UsuarioRolIds is null)
        {
            errores.Agregar("usuarioRolIds", "Indica los jugadores convocados; puede ser una lista vacía.");
        }
        else if (datos.UsuarioRolIds.Distinct().Count() != datos.UsuarioRolIds.Count)
        {
            errores.Agregar("usuarioRolIds", "Hay un jugador repetido.");
        }

        errores.LanzarSiHayErrores();
        return datos.UsuarioRolIds!.ToHashSet();
    }
}
