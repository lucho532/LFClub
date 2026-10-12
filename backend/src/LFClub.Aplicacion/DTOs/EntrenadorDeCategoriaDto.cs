using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un integrante asignado como entrenador de una categoría, tal como lo ve el club.
/// Su responsabilidad es decir quién es, cuál es su único rol en el club y qué equipos dirige.
/// No lleva su correo, su celular ni su documento, y no es lo que ve una familia: para eso está
/// <see cref="EntrenadorParaFamiliaDto"/>.
/// </summary>
/// <param name="UsuarioRolId">Identificador del integrante en este club.</param>
/// <param name="Nombres">Nombres del integrante.</param>
/// <param name="Apellidos">Apellidos del integrante.</param>
/// <param name="Rol">Su rol en el club: ENTRENADOR, DIRECTIVO o PRESIDENTE.</param>
/// <param name="Equipos">Equipos que dirige; vacía si lo es de la categoría en general.</param>
public record EntrenadorDeCategoriaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    Rol Rol,
    IReadOnlyList<EquipoDeReferenciaDto> Equipos);
