using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa la consulta del club elegido por uno de sus integrantes (constitución §7.3).
/// Su responsabilidad es devolver los datos y la identidad del club de la petición.
/// No comprueba la pertenencia ni el estado del club: la autorización ya lo hizo antes de fijar el
/// club de la petición.
/// </summary>
public interface IServicioConsultaClub
{
    /// <summary>El club de la petición, con el rol y el identificador de integrante de quien lo consulta.</summary>
    Task<ClubDto> ObtenerAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
