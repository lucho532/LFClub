using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las fotos de perfil.
/// Su responsabilidad es leer, guardar y quitar la foto de una cuenta. Leer, guardar y quitar
/// reciben el identificador de la cuenta con sesión; además lee la de un PRESIDENTE, DIRECTIVO o
/// ENTRENADOR a través de su integrante del club de la petición (spec 008, §14.2 4.5.0).
/// No ofrece listar fotos ni buscar la de otra persona por ningún otro dato, y nunca entrega por un
/// integrante JUGADOR la foto de perfil de su cuenta.
/// </summary>
public interface IRepositorioFotosPerfil
{
    /// <summary>La foto de la cuenta, o nulo si no tiene.</summary>
    Task<FotoPerfil?> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// La foto de la cuenta de ese integrante del club de la petición, con el filtro de
    /// aislamiento; nulo si no tiene, si el integrante no es de este club, si es JUGADOR o si su
    /// ingreso no está aprobado.
    /// </summary>
    Task<FotoPerfil?> ObtenerDeIntegranteAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Guarda la foto de la cuenta, reemplazando la que hubiera; se guarda con la unidad de trabajo.</summary>
    Task GuardarAsync(Guid usuarioId, byte[] contenido, string tipoContenido, CancellationToken cancelacion = default);

    /// <summary>Quita la foto de la cuenta, si la tiene; se guarda con la unidad de trabajo.</summary>
    Task QuitarAsync(Guid usuarioId, CancellationToken cancelacion = default);
}
