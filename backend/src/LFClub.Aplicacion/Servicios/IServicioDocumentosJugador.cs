using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa los casos de uso de los archivos de la ficha de un jugador: abrir el archivo
/// entregado para un documento pedido y subir o reemplazar ese archivo (RF-027 a RF-031).
/// Su responsabilidad es entregar el archivo solo a quien puede ver los documentos de esa ficha y
/// guardar uno nuevo solo de quien puede cambiarla, dejando registrado el cambio (RF-038).
/// No borra documentos: un documento entregado se reemplaza, no vuelve a quedar pendiente.
/// </summary>
public interface IServicioDocumentosJugador
{
    /// <summary>
    /// El archivo entregado para ese documento, con su tipo de contenido y el nombre con el que se
    /// sirve. Responde 404 <c>no_encontrado</c> si quien pregunta no puede ver esa ficha o sus
    /// documentos, si <paramref name="documento"/> no es un documento pedido o si está pendiente.
    /// </summary>
    Task<(byte[] Contenido, string TipoContenido, string NombreDeArchivo)> AbrirAsync(
        Guid usuarioRolId, string? documento, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda el archivo de ese documento, sustituyendo al que hubiera, y devuelve la ficha. Un
    /// archivo no admitido, vacío o ausente responde 400 <c>archivo_no_admitido</c>; uno de más de
    /// 10 MB, 400 <c>archivo_demasiado_grande</c>. Si se rechaza, el anterior se conserva.
    /// </summary>
    Task<FichaJugadorDto> SubirAsync(
        Guid usuarioRolId,
        string? documento,
        byte[]? contenido,
        UsuarioRol quienSube,
        CancellationToken cancelacion = default);
}
