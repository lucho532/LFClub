using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta del resumen del club para el inicio del PRESIDENTE y de los DIRECTIVOS
/// (spec 007, RF-011 y RF-015).
/// Su responsabilidad es devolver las cuatro cifras del club de la petición, calculadas al
/// consultar.
/// No decide quién puede verlo (la autorización solo deja pasar al PRESIDENTE y a los DIRECTIVOS)
/// ni guarda nada.
/// </summary>
public interface IServicioResumenClub
{
    /// <summary>Las cuatro cifras del club de la petición.</summary>
    Task<ResumenClubDto> ObtenerAsync(CancellationToken cancelacion = default);
}
