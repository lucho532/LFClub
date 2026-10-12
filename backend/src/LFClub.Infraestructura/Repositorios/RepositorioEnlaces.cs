using LFClub.Aplicacion.Interfaces;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa la lectura de integrantes para resolver enlaces a fichas, con Entity Framework.
/// Su responsabilidad es proyectar el identificador, la cuenta, el rol y el estado de ingreso de los
/// integrantes de las cuentas pedidas en una sola consulta. Trabaja siempre con el filtro de
/// aislamiento activo: sin club en el contexto no devuelve nada.
/// No se salta el filtro de aislamiento, no lee nombres ni datos personales y no contiene reglas.
/// </summary>
public class RepositorioEnlaces : IRepositorioEnlaces
{
    private readonly ContextoLFClub _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la petición.</summary>
    public RepositorioEnlaces(ContextoLFClub contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegranteDeEnlace>> IntegrantesDeCuentasAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancelacion = default)
    {
        if (usuarioIds.Count == 0)
        {
            return [];
        }

        return await _contexto.UsuariosRol
            .Where(integrante => usuarioIds.Contains(integrante.UsuarioId))
            .Select(integrante => new IntegranteDeEnlace(
                integrante.Id, integrante.UsuarioId, integrante.Rol, integrante.EstadoIngreso))
            .ToListAsync(cancelacion);
    }
}
