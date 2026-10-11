using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa la resolución de a qué ficha lleva cada nombre de una respuesta (spec 008, RF-010 y
/// RF-011; research §6).
/// Su responsabilidad es aplicar <see cref="ReglaEnlaceDeFicha"/> y armar el enlace. Para los
/// autores copiados recibe todas las cuentas de una lista a la vez y consulta una sola vez a sus
/// integrantes de hoy en el club de la petición: nunca hace una consulta por fila. Para un
/// integrante que la respuesta ya trae cargado no consulta nada.
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
            .Select(integrante => (
                integrante.UsuarioId,
                integrante.UsuarioRolId,
                Tipo: ReglaEnlaceDeFicha.ParaAutor(integrante.Rol, integrante.EstadoIngreso)))
            .Where(fila => fila.Tipo is not null)
            .DistinctBy(fila => fila.UsuarioId)
            .ToDictionary(fila => fila.UsuarioId, fila => new EnlaceFichaDto(fila.UsuarioRolId, fila.Tipo!.Value));
    }

    /// <summary>
    /// Enlace a la ficha de un jugador ya cargado en una planilla o en los goleadores (spec 009): solo
    /// si quien mira puede abrir esa ficha según <see cref="ReglaAccesoAFicha"/>. Para la cuenta de
    /// un jugador es siempre nulo: no abre fichas ajenas. No consulta nada.
    /// </summary>
    public static EnlaceFichaDto? DeJugador(UsuarioRol jugador, QuienPregunta quien)
    {
        if (quien.EsJugador)
        {
            return null;
        }

        var entrenaSuCategoria = quien.Rol == Rol.ENTRENADOR
            && jugador.CategoriaId is { } categoria
            && quien.CategoriasQueEntrena.Contains(categoria);
        var alcance = ReglaAccesoAFicha.Evaluar(quien.Rol, false, jugador.Activo, entrenaSuCategoria);
        return alcance.Ve ? new EnlaceFichaDto(jugador.Id, TipoFicha.JUGADOR) : null;
    }

    /// <summary>
    /// Enlace de un integrante ya cargado según la regla indicada (persona aprobada o hermano), sin
    /// consultar nada: si es nulo, porque ya no existe, no lleva a ninguna ficha.
    /// </summary>
    public static EnlaceFichaDto? DeIntegrante(UsuarioRol? integrante, Func<Rol?, EstadoIngreso?, TipoFicha?> regla) =>
        integrante is not null && regla(integrante.Rol, integrante.EstadoIngreso) is { } tipo
            ? new EnlaceFichaDto(integrante.Id, tipo)
            : null;
}
