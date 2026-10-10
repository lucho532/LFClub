using System.Text.Json.Serialization;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa la ficha de un jugador en un club tal como la recibe quien la consulta (RF-001).
/// Su responsabilidad es llevar la identidad, el contacto, el contacto de emergencia, la seguridad
/// social y, solo para quien puede verlos, los datos clínicos y los documentos; además del último
/// cambio, de lo que quien pregunta puede hacer con ella y de la fecha de la foto del jugador
/// (spec 007), que la pantalla usa para pedirla o mostrar sus iniciales.
/// No lleva en <c>null</c> lo que no se puede ver: <see cref="DatosClinicos"/>,
/// <see cref="Documentos"/> y <see cref="UltimoCambio"/> se omiten del JSON cuando son nulos
/// (RF-010, RF-015). No contiene el contenido de ningún archivo ni la foto.
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador (su integrante) en este club.</param>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="TipoDocumento">Tipo de su documento de identidad.</param>
/// <param name="NumeroDocumento">Número de su documento de identidad.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento.</param>
/// <param name="EsMenorDeEdad">Verdadero si hoy tiene menos de 18 años; entonces el responsable es obligatorio.</param>
/// <param name="Retirado">Verdadero si está retirado del club, lo retirara quien lo retirara.</param>
/// <param name="CategoriaId">Su categoría, o nulo si no tiene.</param>
/// <param name="CategoriaAnio">Año de su categoría, o nulo si no tiene.</param>
/// <param name="Equipos">Nombres de los equipos de su categoría en los que juega.</param>
/// <param name="Contacto">Correo, celular y responsable de la cuenta.</param>
/// <param name="ContactoEmergencia">A quién llamar si le pasa algo.</param>
/// <param name="SeguridadSocial">Entidad de salud y lugar de atención.</param>
/// <param name="DatosClinicos">Datos clínicos; ausente para quien no puede verlos.</param>
/// <param name="Documentos">Los dos documentos pedidos, entregados o pendientes; ausente para quien no puede verlos.</param>
/// <param name="UltimoCambio">Último cambio de la ficha; ausente si nadie la ha cambiado.</param>
/// <param name="Permisos">Lo que quien pregunta puede hacer en esta ficha.</param>
/// <param name="FotoActualizadaEn">Fecha UTC de la última carga de la foto del jugador; nula si no tiene.</param>
public record FichaJugadorDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    TipoDocumento TipoDocumento,
    string NumeroDocumento,
    DateOnly FechaNacimiento,
    bool EsMenorDeEdad,
    bool Retirado,
    Guid? CategoriaId,
    int? CategoriaAnio,
    IReadOnlyList<string> Equipos,
    ContactoDeFichaDto Contacto,
    ContactoEmergenciaDto ContactoEmergencia,
    SeguridadSocialDto SeguridadSocial,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DatosClinicosDto? DatosClinicos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DocumentoDeFichaDto>? Documentos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UltimoCambioDto? UltimoCambio,
    PermisosDeFichaDto Permisos,
    DateTime? FotoActualizadaEn);
