namespace LaPecosa.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador en la lista "Jugadores" (spec 007, RF-005).
/// Su responsabilidad es llevar su nombre, los equipos activos en los que juega y la fecha de su
/// foto, que la pantalla usa para pedirla o mostrar sus iniciales.
/// No lleva su documento, su contacto, sus datos de salud, el estado de su documentación ni ningún
/// dato de pago: "Al día" es un espacio reservado de la pantalla (RF-006). Esos datos están en su
/// ficha, que tiene su propia regla de acceso.
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador (su integrante) en este club.</param>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="Equipos">Nombres de los equipos activos en los que juega, por nombre; vacía es "Sin equipo".</param>
/// <param name="FotoActualizadaEn">Fecha UTC de la última carga de su foto; nula si no tiene.</param>
public record JugadorDeListaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    IReadOnlyList<string> Equipos,
    DateTime? FotoActualizadaEn);
