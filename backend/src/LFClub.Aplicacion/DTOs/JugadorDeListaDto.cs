using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a un jugador en la lista "Jugadores" (spec 007, RF-005).
/// Su responsabilidad es llevar su nombre, los equipos activos en los que juega, la fecha de su
/// foto, que la pantalla usa para pedirla o mostrar sus iniciales, y su estado de cuenta (spec 015,
/// RF-034), que solo se rellena para el PRESIDENTE y el DIRECTIVO: para el ENTRENADOR va nulo y la
/// pantalla no pinta la columna "Al día".
/// No lleva su documento, su contacto, sus datos de salud, el estado de su documentación ni su
/// saldo. Esos datos están en su ficha y en su cuenta, que tienen su propia regla de acceso.
/// </summary>
/// <param name="UsuarioRolId">Identificador del jugador (su integrante) en este club.</param>
/// <param name="Nombres">Nombres del jugador.</param>
/// <param name="Apellidos">Apellidos del jugador.</param>
/// <param name="Equipos">Nombres de los equipos activos en los que juega, por nombre; vacía es "Sin equipo".</param>
/// <param name="FotoActualizadaEn">Fecha UTC de la última carga de su foto; nula si no tiene.</param>
/// <param name="EstadoCuenta">Al día, pago parcial o debe; nulo para quien no ve las finanzas.</param>
public record JugadorDeListaDto(
    Guid UsuarioRolId,
    string Nombres,
    string Apellidos,
    IReadOnlyList<string> Equipos,
    DateTime? FotoActualizadaEn,
    EstadoDeCuenta? EstadoCuenta = null);
