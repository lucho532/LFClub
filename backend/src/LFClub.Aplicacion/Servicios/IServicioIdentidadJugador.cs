using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso que cambian la identidad de un jugador desde su ficha: cambiar su
/// documento de identidad (RF-022 a RF-024) y corregir sus nombres, sus apellidos y su fecha de
/// nacimiento (RF-021, RF-025, RF-026).
/// Su responsabilidad es actualizar siempre al mismo jugador, sin crear otro, dejando registrado
/// quién hizo el cambio y cuándo (RF-038).
/// No cambia el contacto, la salud ni los archivos, ni mueve de categoría a quien ya tiene una.
/// </summary>
public interface IServicioIdentidadJugador
{
    /// <summary>
    /// Cambia el tipo y el número de documento y devuelve la ficha. Número repetido en el club:
    /// 409 <c>documento_repetido_en_club</c>; de otra cuenta en otro club: 409
    /// <c>documento_en_otra_cuenta</c>. Quien no puede ver o cambiar esa ficha recibe 404.
    /// </summary>
    Task<FichaJugadorDto> CambiarDocumentoAsync(
        Guid usuarioRolId,
        CambiarDocumentoIdentidadDto datos,
        UsuarioRol quienCambia,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Corrige los nombres, los apellidos y la fecha de nacimiento y devuelve la ficha. Un jugador
    /// activo sin categoría queda en la categoría activa del año nuevo, si existe. Quien no puede
    /// ver esa ficha o corregir su identidad recibe 404.
    /// </summary>
    Task<FichaJugadorDto> CorregirAsync(
        Guid usuarioRolId, CorregirIdentidadDto datos, UsuarioRol quienCorrige, CancellationToken cancelacion = default);
}
