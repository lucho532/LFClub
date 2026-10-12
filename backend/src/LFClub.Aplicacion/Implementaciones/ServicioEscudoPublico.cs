using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que sirve el escudo de un club.
/// Su responsabilidad es leer el escudo del club de la petición con el filtro de aislamiento
/// activo.
/// No recibe un identificador de club: el controlador público lo fija antes en el contexto.
/// </summary>
public class ServicioEscudoPublico : IServicioEscudoPublico
{
    private readonly IRepositorioEscudos _escudos;

    /// <summary>Crea el servicio con el acceso a los escudos.</summary>
    public ServicioEscudoPublico(IRepositorioEscudos escudos)
    {
        _escudos = escudos;
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(CancellationToken cancelacion = default)
    {
        var escudo = await _escudos.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return (escudo.Contenido, escudo.TipoContenido);
    }
}
