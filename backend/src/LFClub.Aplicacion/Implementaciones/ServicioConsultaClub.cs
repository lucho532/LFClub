using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta el club elegido.
/// Su responsabilidad es leer el club de la petición y convertirlo en DTO.
/// No recibe un identificador de club: solo existe el que fijó la autorización.
/// </summary>
public class ServicioConsultaClub : IServicioConsultaClub
{
    private readonly IRepositorioClub _club;

    /// <summary>Crea el servicio con el acceso al club de la petición.</summary>
    public ServicioConsultaClub(IRepositorioClub club)
    {
        _club = club;
    }

    /// <inheritdoc />
    public async Task<ClubDto> ObtenerAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var club = await _club.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return MapperClub.AClub(club, quienPregunta);
    }
}
