using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa el cuerpo, opcional, de la aprobación de un ingreso (RF-016).
/// Su responsabilidad es dejar que quien llama indique el rol, que solo puede ser JUGADOR: al
/// aprobar no se elige rol y quien estaba en espera entra siempre como JUGADOR.
/// No lleva quién aprueba ni a quién: eso sale de la sesión y de la ruta. Admite cualquier valor
/// de la enumeración de roles para que otro rol se niegue con su propio error en lugar de como un
/// dato mal escrito.
/// </summary>
/// <param name="Rol">Opcional. Si se indica y no es JUGADOR, la aprobación se niega.</param>
public record AprobarIngresoDto(Rol? Rol);
