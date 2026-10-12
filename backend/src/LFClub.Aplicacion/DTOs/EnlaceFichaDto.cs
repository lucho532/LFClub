using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa a dónde lleva un nombre de persona en una pantalla del club (spec 008, RF-010).
/// Su responsabilidad es dar el integrante del club y el tipo de ficha que se abre al pulsar su
/// nombre. Lo pone la API solo cuando quien mira puede abrir esa ficha; es <c>null</c> si no puede
/// o si la persona ya no está en el club (RF-011).
/// No concede acceso: abrir la ficha sigue pasando por las reglas de acceso de cada ficha (RF-012).
/// </summary>
/// <param name="UsuarioRolId">Integrante del club cuya ficha se abre.</param>
/// <param name="Tipo">Ficha del jugador o ficha básica.</param>
public record EnlaceFichaDto(Guid UsuarioRolId, TipoFicha Tipo);
