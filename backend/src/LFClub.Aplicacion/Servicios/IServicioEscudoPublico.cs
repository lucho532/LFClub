namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la lectura pública del escudo de un club (research §10).
/// Su responsabilidad es entregar la imagen del escudo del club de la petición, sin sesión y en
/// cualquier estado del club (supuesto 5): es identidad pública del club.
/// No entrega ningún otro dato del club ni decide nada sobre su sitio público.
/// </summary>
public interface IServicioEscudoPublico
{
    /// <summary>La imagen y su tipo de contenido; 404 <c>no_encontrado</c> si el club no tiene escudo.</summary>
    Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(CancellationToken cancelacion = default);
}
