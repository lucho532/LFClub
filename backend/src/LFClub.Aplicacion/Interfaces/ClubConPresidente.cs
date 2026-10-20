using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa un club de la lista del panel junto con un dato derivado.
/// Su responsabilidad es decir si el club ya tiene algún integrante con rol PRESIDENTE y cuántos
/// integrantes activos tiene por rol.
/// No contiene a los presidentes ni ningún otro integrante.
/// </summary>
/// <param name="Club">El club.</param>
/// <param name="PresidenteRegistrado">Verdadero si algún presidente ya se registró.</param>
/// <param name="Jugadores">Jugadores activos.</param>
/// <param name="Entrenadores">Entrenadores activos.</param>
/// <param name="Directivos">Directivos activos.</param>
/// <param name="Presidentes">Presidentes activos.</param>
public record ClubConPresidente(
    Club Club, bool PresidenteRegistrado, int Jugadores, int Entrenadores, int Directivos, int Presidentes);
