namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa lo que quien ve la carta de un jugador puede hacer con ella: el resultado de
/// <see cref="ReglaAccesoACarta"/> (spec 010).
/// Su responsabilidad es llevar los tres indicadores juntos, para que la consulta, los cambios y la
/// exportación lean la misma decisión.
/// No decide nada ni dice si quien pregunta ve la carta: eso lo decide la visibilidad de la ficha.
/// </summary>
/// <param name="CambiaEstadisticas">Puede poner, cambiar o borrar las seis estadísticas.</param>
/// <param name="CambiaDorsalYPosicion">Puede cambiar el dorsal y la posición.</param>
/// <param name="Exporta">Puede descargar la carta como imagen.</param>
public sealed record AlcanceDeCarta(bool CambiaEstadisticas, bool CambiaDorsalYPosicion, bool Exporta)
{
    /// <summary>Quien solo mira la carta.</summary>
    public static AlcanceDeCarta Ninguno { get; } = new(false, false, false);
}
