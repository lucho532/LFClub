using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de invitaciones e integrantes en los DTO del apartado "Ingresos".
/// Su responsabilidad es que ninguna entidad salga por la API (constitución §5) y que cada DTO
/// lleve solo lo que el PRESIDENTE puede ver.
/// No incluye el token de las invitaciones ni su hash, y no consulta la base de datos.
/// </summary>
public static class MapperIngresos
{
    /// <summary>Invitación del club sin su token, con su estado en ese instante.</summary>
    public static InvitacionClubDto AInvitacionClub(Invitacion invitacion, string? enviadaPor, DateTime ahoraUtc) => new(
        invitacion.Id,
        invitacion.Correo,
        invitacion.Rol,
        invitacion.EstadoEn(ahoraUtc),
        invitacion.EstadoEnvio,
        enviadaPor,
        invitacion.CreadaEn,
        invitacion.VenceEn);

    /// <summary>
    /// Persona de la sala de espera, a partir de un integrante con su cuenta cargada y, si se
    /// agregó desde la ficha de un hermano que sigue en el club, con ese jugador de origen. El
    /// nombre del hermano se lee en el momento: no se copia en ninguna columna, porque solo se
    /// muestra mientras el ingreso está en espera y no es histórico (§13).
    /// <paramref name="retiraDeOtroClub"/> llega ya calculado por el servicio. El hermano lleva a su
    /// ficha si hoy es un JUGADOR aprobado (spec 008).
    /// </summary>
    public static IngresoEnEsperaDto AIngresoEnEspera(UsuarioRol integrante, bool retiraDeOtroClub)
    {
        var cuenta = integrante.Usuario
            ?? throw new InvalidOperationException("El integrante en espera debe venir con su cuenta cargada.");

        return new IngresoEnEsperaDto(
            integrante.Id,
            integrante.Nombres,
            integrante.Apellidos,
            integrante.TipoDocumento,
            integrante.NumeroDocumento,
            integrante.FechaNacimiento,
            cuenta.Correo,
            cuenta.Celular ?? string.Empty,
            cuenta.NombreResponsable,
            integrante.CreadoEn,
            integrante.AgregadoDesde is { } origen ? $"{origen.Nombres} {origen.Apellidos}" : null,
            retiraDeOtroClub,
            ResolutorDeEnlaces.DeIntegrante(integrante.AgregadoDesde, ReglaEnlaceDeFicha.ParaHermano));
    }

    /// <summary>
    /// Ingreso aprobado. Quién aprobó y con qué rol salen de lo copiado al aprobar, nunca del
    /// nombre ni del rol actuales (§13). La persona lleva a su ficha según su rol de hoy, y
    /// <paramref name="aprobadoPorFicha"/> es el enlace ya resuelto a la ficha básica de quien aprobó.
    /// </summary>
    public static IngresoAprobadoDto AIngresoAprobado(UsuarioRol integrante, EnlaceFichaDto? aprobadoPorFicha) => new(
        integrante.Id,
        integrante.Nombres,
        integrante.Apellidos,
        integrante.RolDeIngreso
            ?? throw new InvalidOperationException("Un ingreso aprobado debe tener su rol de ingreso."),
        integrante.AprobadoPorNombre ?? string.Empty,
        integrante.AprobadoEn
            ?? throw new InvalidOperationException("Un ingreso aprobado debe tener su fecha de aprobación."),
        ResolutorDeEnlaces.DeIntegrante(integrante, ReglaEnlaceDeFicha.ParaPersonaAprobada),
        aprobadoPorFicha);
}
