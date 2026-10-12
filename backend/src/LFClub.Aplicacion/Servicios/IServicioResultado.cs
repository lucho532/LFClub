using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de registrar y corregir el resultado de un partido (spec 009,
/// historia 3).
/// Su responsabilidad es que solo lo haga quien gestiona el partido, cuando ya empezó o ya está
/// jugado, con un resultado válido, y que quede quién lo cambió por última vez.
/// No conoce HTTP ni guarda historial del resultado (§18).
/// </summary>
public interface IServicioResultado
{
    /// <summary>Registra o corrige el resultado y responde con el partido jugado.</summary>
    Task<PartidoDto> RegistrarAsync(
        Guid partidoId, RegistrarResultadoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
