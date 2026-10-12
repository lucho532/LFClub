namespace LaPecosa.Dominio.Enumeraciones;

/// <summary>
/// Representa a quién va dirigido un comunicado del club (constitución §14.4; spec 011, RF-004 a
/// RF-007).
/// Su responsabilidad es distinguir las tres audiencias posibles. <c>CLUB</c> no tiene destinos;
/// <c>CATEGORIAS</c> tiene una fila de destino por categoría; <c>EQUIPOS</c> tiene una fila por
/// equipo, todos de una sola categoría.
/// No resuelve quién es destinatario ni quién puede dirigirse a cada audiencia: eso es de la regla
/// de audiencia y del repositorio.
/// </summary>
public enum AlcanceComunicado
{
    /// <summary>Todas las cuentas con un jugador activo en el club, tenga o no categoría.</summary>
    CLUB,

    /// <summary>Las cuentas con un jugador activo en una o más categorías.</summary>
    CATEGORIAS,

    /// <summary>Las cuentas con un jugador activo en uno o más equipos de una categoría.</summary>
    EQUIPOS,
}
