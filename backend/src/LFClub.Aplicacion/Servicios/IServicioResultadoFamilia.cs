using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso con el que la familia de un convocado publica el resultado de un
/// partido que ya empezó o indica lo de su propio jugador (spec 010, historia 1).
/// Su responsabilidad es que solo lo haga la cuenta del jugador convocado, que valga el primer
/// marcador y que nunca toque ni devuelva lo de otro convocado.
/// No conoce HTTP ni guarda historial (§18).
/// </summary>
public interface IServicioResultadoFamilia
{
    /// <summary>Publica el marcador o indica lo del jugador de la petición, y responde con el partido como lo ve la familia.</summary>
    Task<PartidoDto> PublicarAsync(
        Guid partidoId, ResultadoDeFamiliaDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
