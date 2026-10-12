using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de un club en lo que ve uno de sus integrantes.
/// Su responsabilidad es entregar la configuración, la identidad y el rol y el identificador de
/// integrante de quien consulta.
/// No incluye presidentes, invitaciones ni quién cambió el estado: eso es del panel.
/// </summary>
public static class MapperClub
{
    /// <summary>Club tal como lo ve ese integrante suyo.</summary>
    public static ClubDto AClub(Club club, UsuarioRol quienPregunta) => new(
        club.Id,
        club.Nombre,
        club.Sede,
        club.Direccion,
        club.CorreoContacto,
        club.TelefonoContacto,
        club.Estado,
        MapperIdentidadClub.AIdentidad(club),
        quienPregunta.Rol,
        quienPregunta.Id);
}
