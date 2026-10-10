using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso al escudo del club de la petición.
/// Su responsabilidad es leer y guardar el escudo del club fijado en <see cref="IContextoClub"/>,
/// con el filtro de aislamiento activo: el escudo no es una excepción de §7.1. Guardar es lo que usa
/// el PRESIDENTE desde "Datos del club" (spec 007); el panel guarda con su propio repositorio.
/// No recibe un identificador de club, no valida la imagen y no sube la versión del escudo.
/// </summary>
public interface IRepositorioEscudos
{
    /// <summary>El escudo del club de la petición, o nulo si no tiene o no hay club en el contexto.</summary>
    Task<EscudoClub?> ObtenerAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda el escudo del club de la petición, reemplazando el que tuviera; se guarda con la
    /// unidad de trabajo. Sin club en el contexto, falla.
    /// </summary>
    Task GuardarAsync(byte[] contenido, string tipoContenido, CancellationToken cancelacion = default);
}
