using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa los datos personales que un PRESIDENTE, DIRECTIVO o ENTRENADOR ve de sí mismo en
/// "Mi perfil" (spec 008, RF-015; constitución §12.6).
/// Su responsabilidad es llevar sus nombres, apellidos, documento y fecha de nacimiento, tomados de
/// su integrante más antiguo con uno de esos roles, y el correo y el celular de su cuenta.
/// Solo sale para la cuenta con sesión, nunca para otra persona. El documento y el correo son de
/// solo lectura: ninguna operación de "Mi perfil" los cambia (RF-018, RF-019).
/// </summary>
/// <param name="Nombres">Nombres.</param>
/// <param name="Apellidos">Apellidos.</param>
/// <param name="TipoDocumento">Tipo de documento; de solo lectura.</param>
/// <param name="NumeroDocumento">Número de documento; de solo lectura.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="Correo">Correo de la cuenta; de solo lectura.</param>
/// <param name="Celular">Celular de la cuenta.</param>
public record DatosPropiosDto(
    string Nombres,
    string Apellidos,
    TipoDocumento TipoDocumento,
    string NumeroDocumento,
    DateOnly FechaNacimiento,
    string Correo,
    string Celular);
