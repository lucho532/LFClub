using System.Text.Json.Serialization;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador en una lista del club (RF-032 de la 003).
/// Su responsabilidad es llevar su nombre, sus apellidos, su año de nacimiento, sus equipos, si
/// está en una categoría que no es la de su año (RF-016) y, solo para el PRESIDENTE y los
/// DIRECTIVOS, cuántos documentos de su ficha le faltan por entregar (RF-032 de la 005).
/// No lleva su documento, su correo ni su celular: esos datos están en su ficha. Para un
/// ENTRENADOR, <see cref="DocumentosPendientes"/> se omite del JSON: no debe saber si los
/// documentos están entregados o pendientes (RF-007 de la 005).
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante en este club.</param>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="AnioNacimiento">Año de nacimiento.</param>
/// <param name="FueraDeSuAnio">Verdadero si su categoría no es la de su año; falso en "Sin categoría".</param>
/// <param name="Equipos">Equipos de su categoría en los que juega.</param>
/// <param name="DocumentosPendientes">
/// Cuántos documentos pedidos le faltan; 0 es documentación completa. Nulo, y ausente del JSON,
/// para quien no puede ver los documentos.
/// </param>
public record JugadorDeCategoriaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    int AnioNacimiento,
    bool FueraDeSuAnio,
    IReadOnlyList<EquipoDeReferenciaDto> Equipos,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? DocumentosPendientes);
