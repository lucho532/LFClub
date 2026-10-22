using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de un club en los DTO del panel de administración.
/// Su responsabilidad es entregar solo lo que §8 permite ver al DESARROLLADOR.
/// No incluye el token de las invitaciones ni datos personales de los presidentes más allá de su
/// nombre y su correo.
/// </summary>
public static class MapperClubPlataforma
{
    /// <summary>Club de la lista del panel.</summary>
    public static ClubResumenDto AResumen(ClubConPresidente fila, EstadoDePagoClubDto? pago = null) => new(
        fila.Club.Id,
        fila.Club.Nombre,
        fila.Club.Estado,
        fila.PresidenteRegistrado,
        MapperIdentidadClub.AIdentidad(fila.Club),
        new ConteoIntegrantesDto(fila.Jugadores, fila.Entrenadores, fila.Directivos, fila.Presidentes),
        pago?.EstadoDePago ?? EstadoDePagoClub.SIN_COBRO,
        pago?.ValorAdeudado ?? 0,
        pago?.MesDeInicioDelCobro,
        pago?.MotivoDeSuspension);

    /// <summary>Detalle de un club con sus presidentes y sus invitaciones sin usar ni anular.</summary>
    public static ClubDetalleDto ADetalle(
        Club club,
        IEnumerable<UsuarioRol> presidentes,
        IEnumerable<Invitacion> invitaciones,
        DateTime ahoraUtc,
        EstadoDePagoClubDto? pago = null) => new(
        club.Id,
        club.Nombre,
        club.Sede,
        club.Direccion,
        club.CorreoContacto,
        club.TelefonoContacto,
        club.Estado,
        club.EstadoCambiadoEn,
        MapperIdentidadClub.AIdentidad(club),
        presidentes.Select(APresidente).ToList(),
        invitaciones.Select(invitacion => AInvitacion(invitacion, ahoraUtc)).ToList(),
        pago?.EstadoDePago ?? EstadoDePagoClub.SIN_COBRO,
        pago?.ValorAdeudado ?? 0,
        pago?.MesDeInicioDelCobro,
        pago?.MotivoDeSuspension);

    /// <summary>Presidente registrado, a partir de un integrante con su cuenta cargada.</summary>
    public static PresidenteDto APresidente(UsuarioRol integrante) => new(
        integrante.Id,
        integrante.Nombres,
        integrante.Apellidos,
        integrante.Usuario?.Correo
            ?? throw new InvalidOperationException("El presidente debe venir con su cuenta cargada."));

    /// <summary>Invitación sin su token.</summary>
    public static InvitacionDto AInvitacion(Invitacion invitacion, DateTime ahoraUtc) => new(
        invitacion.Id,
        invitacion.Correo,
        invitacion.Rol,
        invitacion.EstadoEnvio,
        invitacion.CreadaEn,
        invitacion.VenceEn,
        invitacion.EstaVencida(ahoraUtc));
}
