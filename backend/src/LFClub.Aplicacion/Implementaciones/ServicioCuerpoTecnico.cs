using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio del cuerpo técnico del club para su PRESIDENTE (spec 018).
/// Su responsabilidad es leer a los integrantes que no son jugadores con sus categorías y equipos, armar la
/// lista ordenada por rol y nombre, y, en el detalle de un ENTRENADOR, los próximos entrenamientos que ve por
/// sus categorías, con la misma regla de visibilidad que usa su propio inicio.
/// No comprueba quién pregunta (lo hace la autorización) y no modifica datos.
/// </summary>
public class ServicioCuerpoTecnico : IServicioCuerpoTecnico
{
    private static readonly Rol[] OrdenDeRoles = [Rol.PRESIDENTE, Rol.DIRECTIVO, Rol.ENTRENADOR];

    private readonly IRepositorioCuerpoTecnico _cuerpo;
    private readonly IRepositorioEntrenamientos _entrenamientos;
    private readonly AccesoAActividad _acceso;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCuerpoTecnico(
        IRepositorioCuerpoTecnico cuerpo,
        IRepositorioEntrenamientos entrenamientos,
        AccesoAActividad acceso,
        IReloj reloj)
    {
        _cuerpo = cuerpo;
        _entrenamientos = entrenamientos;
        _acceso = acceso;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MiembroCuerpoTecnicoDto>> ListarAsync(CancellationToken cancelacion = default)
    {
        var miembros = await _cuerpo.ListarAsync(cancelacion);
        var asignaciones = (await _cuerpo.AsignacionesActivasAsync(
                miembros.Select(miembro => miembro.Integrante.Id).ToList(), cancelacion))
            .ToLookup(asignacion => asignacion.UsuarioRolId);

        return miembros
            .OrderBy(miembro => Array.IndexOf(OrdenDeRoles, miembro.Integrante.Rol))
            .ThenBy(miembro => miembro.Integrante.Apellidos, OrdenDePersonas.Comparador)
            .ThenBy(miembro => miembro.Integrante.Nombres, OrdenDePersonas.Comparador)
            .Select(miembro => new MiembroCuerpoTecnicoDto(
                miembro.Integrante.Id,
                miembro.Integrante.Nombres,
                miembro.Integrante.Apellidos,
                miembro.Integrante.Rol,
                miembro.VersionFoto,
                MapperFichaBasica.ACategorias(asignaciones[miembro.Integrante.Id])))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MiembroCuerpoTecnicoDetalleDto> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var miembro = await _cuerpo.ObtenerAsync(usuarioRolId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var asignaciones = await _cuerpo.AsignacionesActivasAsync([usuarioRolId], cancelacion);

        return new MiembroCuerpoTecnicoDetalleDto(
            miembro.Integrante.Id,
            miembro.Integrante.Nombres,
            miembro.Integrante.Apellidos,
            miembro.Integrante.Rol,
            miembro.VersionFoto,
            miembro.Integrante.TipoDocumento,
            miembro.Integrante.NumeroDocumento,
            miembro.Correo,
            miembro.Celular,
            MapperFichaBasica.ACategorias(asignaciones),
            miembro.Integrante.Rol == Rol.ENTRENADOR ? await ProximosAsync(miembro.Integrante, cancelacion) : []);
    }

    /// <summary>Los entrenamientos que no han terminado y que ese entrenador ve por sus categorías.</summary>
    private async Task<IReadOnlyList<EntrenamientoResumenDto>> ProximosAsync(
        UsuarioRol entrenador, CancellationToken cancelacion)
    {
        var quien = await _acceso.QuienPreguntaAsync(entrenador, cancelacion);
        var (hoy, ahora) = HoraDeColombia.Ahora(_reloj);
        var proximos = await _entrenamientos.ProximosVisiblesAsync(
            hoy, ahora, quien, ServicioConsultaEntrenamientos.MaximoDeProximos, cancelacion);

        return await ServicioConsultaEntrenamientos.ResumenesAsync(_entrenamientos, proximos, quien, cancelacion);
    }
}
