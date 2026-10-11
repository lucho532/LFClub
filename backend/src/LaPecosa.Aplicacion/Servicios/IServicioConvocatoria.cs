using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de la planilla y la convocatoria de un partido (spec 009, RF-009,
/// RF-010, RF-012, RF-013 y RF-034).
/// Su responsabilidad es mostrar la planilla solo al PRESIDENTE y a quien entrena alguna de sus
/// categorías, y dejar la convocatoria exactamente con los jugadores pedidos, si quien la guarda
/// gestiona el partido y cada jugador es activo y de una de sus categorías.
/// No conoce HTTP y nunca entrega la planilla a la cuenta de un jugador.
/// </summary>
public interface IServicioConvocatoria
{
    /// <summary>La planilla del partido.</summary>
    Task<PlanillaPartidoDto> PlanillaAsync(Guid partidoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Deja la convocatoria con esos jugadores y responde con la planilla actualizada.</summary>
    Task<PlanillaPartidoDto> GuardarAsync(
        Guid partidoId, GuardarConvocatoriaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
