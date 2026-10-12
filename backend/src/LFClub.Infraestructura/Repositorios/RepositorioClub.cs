using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LFClub.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso al club de la petición con Entity Framework.
/// Su responsabilidad es leer y bloquear únicamente el club fijado en <see cref="IContextoClub"/>.
/// No toca ningún otro club: sin club en el contexto devuelve nulo (falla cerrado).
/// </summary>
public class RepositorioClub : IRepositorioClub
{
    private readonly ContextoLFClub _contexto;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el repositorio sobre el contexto de datos y el club de la petición.</summary>
    public RepositorioClub(ContextoLFClub contexto, IContextoClub contextoClub)
    {
        _contexto = contexto;
        _contextoClub = contextoClub;
    }

    /// <inheritdoc />
    public async Task<Guid> BloquearAsync(CancellationToken cancelacion = default)
    {
        var clubId = _contextoClub.ClubId
            ?? throw new InvalidOperationException("No hay club en el contexto de la petición.");

        // El mismo recurso que bloquea el retiro de un presidente desde el panel.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Clubes\" WHERE \"Id\" = {clubId} FOR UPDATE", cancelacion);
        return clubId;
    }

    /// <inheritdoc />
    public async Task<Club?> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var clubId = _contextoClub.ClubId;
        return clubId is null
            ? null
            : await _contexto.Clubes.FirstOrDefaultAsync(club => club.Id == clubId, cancelacion);
    }
}
