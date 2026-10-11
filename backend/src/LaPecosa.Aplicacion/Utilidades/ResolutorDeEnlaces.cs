using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa la resolución de a qué ficha lleva cada nombre de una respuesta (spec 008, RF-010 y
/// RF-011; research §6).
/// Su responsabilidad es recibir todos los identificadores de una lista a la vez, consultar una sola
/// vez a los integrantes de hoy del club de la petición y aplicar <see cref="ReglaEnlaceDeFicha"/>,
/// devolviendo el enlace de cada uno que lleve a alguna ficha. Nunca hace una consulta por fila.
/// No concede acceso (abrir la ficha sigue pasando por sus reglas, RF-012) y no cambia el nombre
/// que se muestra: el enlace lleva a la persona de hoy y el nombre sigue siendo el copiado (§13).
/// </summary>
public class ResolutorDeEnlaces
{
    private readonly IRepositorioEnlaces _enlaces;

    /// <summary>Crea el resolutor sobre la lectura de integrantes.</summary>
    public ResolutorDeEnlaces(IRepositorioEnlaces enlaces)
    {
        _enlaces = enlaces;
    }

    /// <summary>
    /// Enlace de cada autor copiado (retiro, aprobación, último cambio), por su cuenta. Solo salen
    /// las cuentas que hoy tienen en el club un integrante PRESIDENTE, DIRECTIVO o ENTRENADOR
    /// aprobado; los nulos se ignoran.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, EnlaceFichaDto>> AutoresAsync(
        IEnumerable<Guid?> usuarioIds, CancellationToken cancelacion = default)
    {
        var buscadas = usuarioIds.OfType<Guid>().ToHashSet();
        var integrantes = await _enlaces.IntegrantesDeCuentasAsync(buscadas, cancelacion);

        return integrantes
            .Select(integrante => (integrante.UsuarioId, Tipo: ReglaEnlaceDeFicha.ParaAutor(integrante.Rol, integrante.EstadoIngreso), integrante.UsuarioRolId))
            .Where(fila => fila.Tipo is not null)
            .DistinctBy(fila => fila.UsuarioId)
            .ToDictionary(fila => fila.UsuarioId, fila => new EnlaceFichaDto(fila.UsuarioRolId, fila.Tipo!.Value));
    }

    /// <summary>
    /// Enlace de cada persona de un ingreso aprobado, por su identificador, según su integrante de
    /// hoy: ficha del jugador o ficha básica. Las que ya no existen no salen.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, EnlaceFichaDto>> PersonasAprobadasAsync(
        IEnumerable<Guid> usuarioRolIds, CancellationToken cancelacion = default) =>
        PorIdentificadorAsync(usuarioRolIds.Cast<Guid?>(), ReglaEnlaceDeFicha.ParaPersonaAprobada, cancelacion);

    /// <summary>
    /// Enlace de cada hermano desde cuya ficha se agregó a alguien en espera, por su identificador:
    /// solo si hoy es un JUGADOR aprobado. Los nulos se ignoran.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, EnlaceFichaDto>> HermanosAsync(
        IEnumerable<Guid?> usuarioRolIds, CancellationToken cancelacion = default) =>
        PorIdentificadorAsync(usuarioRolIds, ReglaEnlaceDeFicha.ParaHermano, cancelacion);

    private async Task<IReadOnlyDictionary<Guid, EnlaceFichaDto>> PorIdentificadorAsync(
        IEnumerable<Guid?> usuarioRolIds,
        Func<Rol?, EstadoIngreso?, TipoFicha?> regla,
        CancellationToken cancelacion)
    {
        var buscados = usuarioRolIds.OfType<Guid>().ToHashSet();
        var integrantes = await _enlaces.IntegrantesAsync(buscados, cancelacion);

        return integrantes
            .Select(integrante => (integrante.UsuarioRolId, Tipo: regla(integrante.Rol, integrante.EstadoIngreso)))
            .Where(fila => fila.Tipo is not null)
            .ToDictionary(fila => fila.UsuarioRolId, fila => new EnlaceFichaDto(fila.UsuarioRolId, fila.Tipo!.Value));
    }
}
