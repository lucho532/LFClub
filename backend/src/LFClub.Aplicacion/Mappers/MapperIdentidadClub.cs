using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de un club en su identidad visual.
/// Su responsabilidad es entregar los colores y la dirección del escudo con su versión, para que
/// el navegador pueda guardarlo en caché.
/// No lee el escudo ni decide cómo se pinta.
/// </summary>
public static class MapperIdentidadClub
{
    /// <summary>Identidad del club; la dirección del escudo es nula si no tiene.</summary>
    public static IdentidadClubDto AIdentidad(Club club) => new(
        club.ColorPrincipal,
        club.ColorAcento,
        club.VersionEscudo == 0 ? null : $"/api/publico/clubes/{club.Id}/escudo?v={club.VersionEscudo}");
}
