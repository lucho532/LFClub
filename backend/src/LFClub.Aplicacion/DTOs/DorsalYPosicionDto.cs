using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que se envía al poner o cambiar el dorsal y la posición de la carta (spec 010,
/// RF-027).
/// Su responsabilidad es llevar los dos valores; nulo quita cada uno.
/// No valida nada: lo hace el servicio. Una posición que no es de la lista no se puede leer y
/// responde 400 <c>datos_invalidos</c> antes de llegar al servicio.
/// </summary>
/// <param name="Dorsal">Entero de 1 a 99, o nulo.</param>
/// <param name="Posicion">Una de las cuatro posiciones, o nula.</param>
public record DorsalYPosicionDto(int? Dorsal, PosicionJugador? Posicion);
