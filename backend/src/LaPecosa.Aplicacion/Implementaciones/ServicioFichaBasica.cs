using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la ficha básica y de la foto de perfil ante el club (spec 008).
/// Su responsabilidad es leer al integrante y sus asignaciones del club de la petición y convertirlos
/// en la ficha básica, y entregar la foto de su cuenta solo si tiene ficha básica en este club.
/// No comprueba la pertenencia de quien pregunta, que ya resolvió la autorización, no modifica datos
/// y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioFichaBasica : IServicioFichaBasica
{
    private readonly IRepositorioFichaBasica _fichas;
    private readonly IRepositorioFotosPerfil _fotos;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioFichaBasica(IRepositorioFichaBasica fichas, IRepositorioFotosPerfil fotos)
    {
        _fichas = fichas;
        _fotos = fotos;
    }

    /// <inheritdoc />
    public async Task<FichaBasicaDto> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var persona = await _fichas.ObtenerAsync(usuarioRolId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        var asignaciones = await _fichas.AsignacionesActivasAsync(usuarioRolId, cancelacion);
        return MapperFichaBasica.AFichaBasica(persona, asignaciones);
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido)> ObtenerFotoAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var foto = await _fotos.ObtenerDeIntegranteAsync(usuarioRolId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        return (foto.Contenido, foto.TipoContenido);
    }
}
