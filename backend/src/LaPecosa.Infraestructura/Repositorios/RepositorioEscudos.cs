using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace LaPecosa.Infraestructura.Repositorios;

/// <summary>
/// Representa el acceso al escudo del club de la petición con Entity Framework.
/// Su responsabilidad es leer y guardar el escudo del club de la petición. No filtra por club a
/// mano al leer: lo hace el filtro global, que sin club en el contexto no devuelve ninguna fila; al
/// crear el escudo, lo asigna al club del contexto.
/// No se salta el filtro de aislamiento ni contiene reglas de negocio.
/// </summary>
public class RepositorioEscudos : IRepositorioEscudos
{
    private readonly ContextoLaPecosa _contexto;
    private readonly IContextoClub _contextoClub;

    /// <summary>Crea el repositorio sobre el contexto de datos y el club de la petición.</summary>
    public RepositorioEscudos(ContextoLaPecosa contexto, IContextoClub contextoClub)
    {
        _contexto = contexto;
        _contextoClub = contextoClub;
    }

    /// <inheritdoc />
    public Task<EscudoClub?> ObtenerAsync(CancellationToken cancelacion = default) =>
        _contexto.EscudosClub.AsNoTracking().FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task GuardarAsync(byte[] contenido, string tipoContenido, CancellationToken cancelacion = default)
    {
        var clubId = _contextoClub.ClubId
            ?? throw new InvalidOperationException("No hay club en el contexto de la petición.");
        var escudo = await _contexto.EscudosClub.FirstOrDefaultAsync(cancelacion);
        if (escudo is null)
        {
            _contexto.EscudosClub.Add(new EscudoClub { ClubId = clubId, Contenido = contenido, TipoContenido = tipoContenido });
            return;
        }

        escudo.Contenido = contenido;
        escudo.TipoContenido = tipoContenido;
    }
}
