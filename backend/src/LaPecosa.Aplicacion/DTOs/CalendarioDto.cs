namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa lo que quien pregunta puede ver de un rango de días: entrenamientos y partidos, por
/// fecha y hora (spec 009, research §12).
/// Su responsabilidad es llevar el rango pedido y las dos listas.
/// No lleva nada que quien pregunta no pueda ver: un partido próximo al que no está convocado no
/// aparece, ni en la lista ni en ningún total (§17.2).
/// </summary>
/// <param name="Desde">Primer día del rango.</param>
/// <param name="Hasta">Último día del rango.</param>
/// <param name="Entrenamientos">Entrenamientos visibles del rango.</param>
/// <param name="Partidos">Partidos visibles del rango.</param>
public record CalendarioDto(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyList<EntrenamientoResumenDto> Entrenamientos,
    IReadOnlyList<PartidoResumenDto> Partidos);
