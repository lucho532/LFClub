using LaPecosa.Aplicacion.DTOs;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de la foto de perfil de una cuenta (RF-036 a RF-038).
/// Su responsabilidad es que cada cuenta cargue, cambie, quite y vea su propia foto.
/// No permite ver ni cambiar la foto de otra cuenta: todas las operaciones son sobre la cuenta con
/// sesión y ninguna recibe el identificador de otra.
/// </summary>
public interface IServicioFotoPerfil
{
    /// <summary>La foto de la cuenta y su tipo de contenido; 404 <c>no_encontrado</c> si no tiene.</summary>
    Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda o reemplaza la foto. Si no es PNG, JPEG ni WebP: 400 <c>foto_no_es_imagen</c>; si pasa
    /// de 1 MB: 400 <c>foto_demasiado_grande</c>. En ambos casos se conserva la anterior. Devuelve
    /// la sesión con la misma <paramref name="limitacion"/> con la que se pidió.
    /// </summary>
    Task<SesionDto> GuardarAsync(
        Guid usuarioId,
        byte[]? contenido,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default);

    /// <summary>Quita la foto; no falla si no había ninguna.</summary>
    Task QuitarAsync(Guid usuarioId, CancellationToken cancelacion = default);
}
