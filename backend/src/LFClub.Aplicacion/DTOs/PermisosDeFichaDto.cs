namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que quien pregunta puede hacer en una ficha.
/// Su responsabilidad es que la pantalla muestre o esconda sus acciones sin decidir por el rol.
/// No es la barrera: la API comprueba igualmente cada operación (§15).
/// </summary>
/// <param name="PuedeCambiar">Contacto, salud, documento de identidad y archivos. PRESIDENTE y cuenta del jugador.</param>
/// <param name="PuedeCorregirIdentidad">Nombres, apellidos y fecha de nacimiento. Solo PRESIDENTE.</param>
public record PermisosDeFichaDto(bool PuedeCambiar, bool PuedeCorregirIdentidad);
