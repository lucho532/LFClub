using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta de la ficha de un jugador (RF-005 a RF-012).
/// Su responsabilidad es devolver la ficha con lo que el rol de quien pregunta permite ver. La usan
/// también las operaciones que cambian la ficha, para responder con la ficha ya guardada.
/// No modifica nada ni entrega el contenido de ningún archivo.
/// </summary>
public interface IServicioConsultaFicha
{
    /// <summary>
    /// La ficha de ese jugador. Quien no puede verla, o un identificador que no es de un jugador
    /// aprobado del club, recibe el mismo 404 <c>no_encontrado</c>.
    /// </summary>
    Task<FichaJugadorDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
