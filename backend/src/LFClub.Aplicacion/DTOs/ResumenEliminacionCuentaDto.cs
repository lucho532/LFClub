namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que la pantalla "Eliminar mi cuenta" muestra antes de confirmar (spec 014, RF-003
/// y RF-009).
/// Su responsabilidad es llevar si la cuenta puede eliminarse, los jugadores que se irían con ella
/// y los clubes donde la persona es el único PRESIDENTE.
/// No lleva documentos, fechas de nacimiento ni datos de contacto: solo nombres y clubes.
/// </summary>
/// <param name="PuedeEliminar">Falso si algún club se quedaría sin presidente.</param>
/// <param name="Jugadores">Todos los jugadores de la cuenta, de cualquier club y estado.</param>
/// <param name="ClubesQueLoImpiden">Clubes donde es el único PRESIDENTE.</param>
public record ResumenEliminacionCuentaDto(
    bool PuedeEliminar,
    IReadOnlyList<JugadorQueSeEliminaDto> Jugadores,
    IReadOnlyList<ClubQueImpideDto> ClubesQueLoImpiden);
