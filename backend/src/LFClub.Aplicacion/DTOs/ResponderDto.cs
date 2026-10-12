using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa lo que la familia responde ante un entrenamiento o un partido (spec 009, RF-023).
/// Su responsabilidad es llevar la respuesta.
/// No dice sobre qué jugador: siempre es el de la petición.
/// </summary>
/// <param name="Respuesta">Asistirá o no asistirá.</param>
public record ResponderDto(RespuestaFamilia? Respuesta);
