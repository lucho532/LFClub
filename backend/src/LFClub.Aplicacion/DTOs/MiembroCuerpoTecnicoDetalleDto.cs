using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa todos los datos de una persona del cuerpo técnico que ve su PRESIDENTE (spec 018).
/// Su responsabilidad es llevar su nombre completo, su documento, su correo, su celular, las categorías y
/// equipos que entrena y, si es ENTRENADOR, sus próximos entrenamientos.
/// Es un dato personal: solo lo entrega el endpoint del PRESIDENTE del club, nunca otro rol ni otro club.
/// </summary>
/// <param name="UsuarioRolId">Integrante de este club.</param>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="Rol">PRESIDENTE, DIRECTIVO o ENTRENADOR.</param>
/// <param name="VersionFoto">Versión de su foto de perfil; 0 si no tiene.</param>
/// <param name="TipoDocumento">Tipo de su documento de identidad.</param>
/// <param name="NumeroDocumento">Número de su documento de identidad (la cédula).</param>
/// <param name="Correo">Correo electrónico de su cuenta.</param>
/// <param name="Celular">Celular de su cuenta; nulo si no lo tiene.</param>
/// <param name="Categorias">Categorías activas que entrena, con los equipos que dirige.</param>
/// <param name="ProximosEntrenamientos">Entrenamientos que no han terminado de sus categorías; vacía si no es ENTRENADOR.</param>
public record MiembroCuerpoTecnicoDetalleDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    Rol Rol,
    int VersionFoto,
    TipoDocumento TipoDocumento,
    string NumeroDocumento,
    string Correo,
    string? Celular,
    IReadOnlyList<CategoriaDeFichaBasicaDto> Categorias,
    IReadOnlyList<EntrenamientoResumenDto> ProximosEntrenamientos);
