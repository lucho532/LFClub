namespace LaPecosa.Aplicacion.Utilidades;

/// <summary>
/// Representa los errores del contrato propios de la ficha del jugador: los de sus archivos, los
/// del cambio de su documento de identidad y los de su foto (spec 007).
/// Su responsabilidad es que todos los casos de uso de la ficha respondan con el mismo código y
/// con un texto que le diga a la persona qué puede hacer.
/// No decide cuándo se produce cada error ni lleva el de una ficha que no se puede ver, que es el
/// <c>no_encontrado</c> común.
/// </summary>
public static class ErroresDeFicha
{
    /// <summary>400: el archivo supera el tamaño máximo (RF-030).</summary>
    public static ExcepcionDeAplicacion ArchivoDemasiadoGrande() => new(
        "archivo_demasiado_grande", 400, "El archivo pesa más de 10 MB.");

    /// <summary>
    /// 400: el archivo no es de un formato admitido, o no se envió ninguno (RF-030).
    /// </summary>
    public static ExcepcionDeAplicacion ArchivoNoAdmitido() => new(
        "archivo_no_admitido", 400, "El archivo debe ser un PDF o una imagen JPEG, PNG o WebP.");

    /// <summary>
    /// 409: ese número de documento ya lo tiene otro integrante del club, activo o retirado
    /// (RF-023).
    /// </summary>
    public static ExcepcionDeAplicacion DocumentoRepetidoEnClub() => ExcepcionDeAplicacion.Conflicto(
        "documento_repetido_en_club", "Ese documento ya está registrado en el club.");

    /// <summary>
    /// 409: ese número de documento ya lo usa otra cuenta en otro club. Al registrarse y al cambiar
    /// el documento desde la ficha, con cualquier rol; al agregar o aprobar un hermano, solo cuando
    /// la otra cuenta lo tiene en un rol que no es JUGADOR (RF-039 de la 006): el de un jugador se
    /// admite y su club lo pierde al aprobarse.
    /// </summary>
    public static ExcepcionDeAplicacion DocumentoEnOtraCuenta() => ExcepcionDeAplicacion.Conflicto(
        "documento_en_otra_cuenta", "Ese documento ya está registrado con otra cuenta.");

    /// <summary>400: la foto del jugador pesa más de 1 MB (spec 007). El mismo texto que la foto de perfil.</summary>
    public static ExcepcionDeAplicacion FotoDemasiadoGrande() => new(
        "foto_demasiado_grande", 400, "La foto pesa más de 1 MB. Usa una imagen más liviana.");

    /// <summary>
    /// 400: la foto del jugador no es una imagen PNG, JPEG o WebP, o no se envió ninguna (spec 007).
    /// El mismo texto que la foto de perfil.
    /// </summary>
    public static ExcepcionDeAplicacion FotoNoEsImagen() => new(
        "foto_no_es_imagen", 400, "La foto debe ser una imagen PNG, JPEG o WebP.");

    /// <summary>409: un jugador retirado no tiene foto; no se le puede cargar (spec 007, §14.2).</summary>
    public static ExcepcionDeAplicacion JugadorRetirado() => ExcepcionDeAplicacion.Conflicto(
        "jugador_retirado", "Este jugador está retirado del club: no se le puede cargar una foto.");
}
