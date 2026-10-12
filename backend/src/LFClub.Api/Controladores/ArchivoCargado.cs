namespace LFClub.Api.Controladores;

/// <summary>
/// Representa la lectura de un archivo cargado en un formulario <c>multipart/form-data</c>.
/// Su responsabilidad es entregar sus bytes a los servicios, que son los que validan el archivo:
/// la imagen de un escudo o de una foto de perfil, o un documento de la ficha de un jugador.
/// No valida el formato ni confía en el nombre o el tipo que declara el navegador.
/// </summary>
public static class ArchivoCargado
{
    /// <summary>
    /// Límite de la petición, por encima de 1 MB para que sea el validador de imágenes, y no el
    /// servidor, el que responda con el motivo cuando un archivo es demasiado grande.
    /// </summary>
    public const int LimiteDePeticion = 5 * 1024 * 1024;

    /// <summary>
    /// Límite de la petición que sube un documento de la ficha de un jugador, por encima de los
    /// 10 MB que admite cada archivo, por el mismo motivo: que sea el validador quien explique el
    /// rechazo.
    /// </summary>
    public const int LimiteDePeticionDeDocumento = 12 * 1024 * 1024;

    /// <summary>Bytes del archivo, o nulo si no se envió ninguno.</summary>
    public static async Task<byte[]?> LeerAsync(IFormFile? archivo, CancellationToken cancelacion)
    {
        if (archivo is null)
        {
            return null;
        }

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, cancelacion);
        return memoria.ToArray();
    }
}
