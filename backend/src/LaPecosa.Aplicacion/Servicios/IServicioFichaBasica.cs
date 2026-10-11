using LaPecosa.Aplicacion.DTOs;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de la ficha básica de un PRESIDENTE, DIRECTIVO o ENTRENADOR del club, y de
/// su foto de perfil ante el club (spec 008, RF-001 a RF-009; constitución §12.6 y §14.2).
/// Su responsabilidad es entregarlas a cualquier integrante del club que pueda entrar en él, también
/// a la familia, y responder como si no existieran cuando la persona es de otro club, es JUGADOR o
/// no existe.
/// No decide quién entra en el club (eso lo hace la autorización de siempre), no modifica datos y
/// no entrega nunca documento, fecha de nacimiento ni contacto.
/// </summary>
public interface IServicioFichaBasica
{
    /// <summary>
    /// La ficha básica de ese integrante. Responde 404 <c>no_encontrado</c> si no es un PRESIDENTE,
    /// DIRECTIVO o ENTRENADOR aprobado de este club, sin distinguir los casos.
    /// </summary>
    Task<FichaBasicaDto> ObtenerAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// La foto de perfil de la cuenta de ese integrante, con su tipo de contenido. Responde 404
    /// <c>no_encontrado</c> si no tiene foto o si no tiene ficha básica en este club, sin distinguir
    /// los casos: la foto de una cuenta que aquí es JUGADOR no sale por esta vía (RF-008).
    /// </summary>
    Task<(byte[] Contenido, string TipoContenido)> ObtenerFotoAsync(
        Guid usuarioRolId, CancellationToken cancelacion = default);
}
