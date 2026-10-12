namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que quien ve una carta puede hacer con ella (spec 010; research §8).
/// Su responsabilidad es que la pantalla muestre o esconda sus formularios y la descarga sin decidir
/// por el rol.
/// No es la barrera: la API comprueba igualmente cada operación (§15).
/// </summary>
/// <param name="CambiaEstadisticas">Poner, cambiar o quitar las seis estadísticas.</param>
/// <param name="CambiaDorsalYPosicion">Cambiar el dorsal y la posición.</param>
/// <param name="Exporta">Descargar la carta como imagen.</param>
public record PermisosDeCartaDto(bool CambiaEstadisticas, bool CambiaDorsalYPosicion, bool Exporta);
