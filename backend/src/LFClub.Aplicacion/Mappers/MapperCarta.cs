using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de la carta de un jugador en el DTO del contrato (spec 010).
/// Su responsabilidad es armar la carta con lo guardado, la valoración calculada, la rareza y los
/// permisos, dejando fuera lo que no hay.
/// No decide quién ve qué ni consulta nada: lo que no se le pasa no sale.
/// </summary>
public static class MapperCarta
{
    /// <summary>La carta del jugador; sin fila guardada, sin dorsal, posición ni números.</summary>
    public static CartaDto ACarta(
        UsuarioRol jugador,
        CartaJugador? carta,
        DateTime? fotoActualizadaEn,
        RarezaCarta rareza,
        AlcanceDeCarta alcance,
        UltimoCambioDto? ultimoCambioEstadisticas) =>
        new(
            jugador.Id,
            jugador.Nombres,
            jugador.Apellidos,
            fotoActualizadaEn,
            carta?.Dorsal,
            carta?.Posicion,
            Valores(carta),
            carta is null
                ? null
                : ReglaValoracionDeCarta.Calcular(carta.Ritmo, carta.Tiro, carta.Pase, carta.Regate, carta.Defensa, carta.Fisico),
            rareza,
            new PermisosDeCartaDto(alcance.CambiaEstadisticas, alcance.CambiaDorsalYPosicion, alcance.Exporta),
            ultimoCambioEstadisticas);

    /// <summary>Las seis estadísticas, o nulas si la carta no las tiene.</summary>
    public static ValoresCartaDto? Valores(CartaJugador? carta) => carta is { TieneEstadisticas: true }
        ? new ValoresCartaDto(
            carta.Ritmo!.Value, carta.Tiro!.Value, carta.Pase!.Value, carta.Regate!.Value, carta.Defensa!.Value, carta.Fisico!.Value)
        : null;
}
