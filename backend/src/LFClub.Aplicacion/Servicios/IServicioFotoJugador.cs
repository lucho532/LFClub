using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso de la foto propia de un jugador: verla, cargarla o cambiarla y
/// quitarla (spec 007, RF-008 a RF-010; constitución §14.2).
/// Su responsabilidad es entregar la foto solo a quien puede ver la ficha de ese jugador y
/// cambiarla solo a quien puede cambiar esa ficha, dejando registrado el cambio.
/// No es la foto de perfil de la cuenta, que sigue siendo privada de su dueño.
/// </summary>
public interface IServicioFotoJugador
{
    /// <summary>
    /// La foto de ese jugador, con su tipo de contenido. Responde 404 <c>no_encontrado</c> si quien
    /// pregunta no puede ver su ficha o si no tiene foto, sin distinguir un caso del otro.
    /// </summary>
    Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda la foto de ese jugador, sustituyendo a la que hubiera, y devuelve su ficha. Una imagen
    /// no admitida o ausente responde 400 <c>foto_no_es_imagen</c>; una de más de 1 MB, 400
    /// <c>foto_demasiado_grande</c>; un jugador retirado, 409 <c>jugador_retirado</c>. Si se
    /// rechaza, la anterior se conserva.
    /// </summary>
    Task<FichaJugadorDto> GuardarAsync(
        Guid usuarioRolId, byte[]? contenido, UsuarioRol quienCarga, CancellationToken cancelacion = default);

    /// <summary>Quita la foto de ese jugador, si la tenía, y devuelve su ficha.</summary>
    Task<FichaJugadorDto> QuitarAsync(Guid usuarioRolId, UsuarioRol quienQuita, CancellationToken cancelacion = default);
}
