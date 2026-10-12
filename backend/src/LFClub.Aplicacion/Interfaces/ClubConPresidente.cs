using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa un club de la lista del panel junto con un dato derivado.
/// Su responsabilidad es decir si el club ya tiene algún integrante con rol PRESIDENTE.
/// No contiene a los presidentes ni ningún otro integrante.
/// </summary>
/// <param name="Club">El club.</param>
/// <param name="PresidenteRegistrado">Verdadero si algún presidente ya se registró.</param>
public record ClubConPresidente(Club Club, bool PresidenteRegistrado);
