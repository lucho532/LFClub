using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Mappers;

/// <summary>
/// Representa la conversión de una cuenta y sus integrantes en los DTO de la sesión.
/// Su responsabilidad es que ninguna entidad salga por la API (constitución §5) y que la sesión
/// traiga una entrada por club: cuando la cuenta tiene varios jugadores en uno (hermanos), los
/// reúne en esa entrada, con la lista para elegir, o deja solo al de la limitación si la sesión se
/// inició con su documento (research §4 de la 006).
/// No incluye la contraseña ni su hash, no consulta la base de datos y no decide quién hace cada
/// petición: eso es de la autorización.
/// </summary>
public static class MapperSesion
{
    /// <summary>
    /// Sesión de una cuenta con los clubes a los que pertenece, en el orden en que llegan.
    /// <paramref name="limitacion"/> son los integrantes a los que está limitada la sesión, o nulo
    /// si llega a todos los de la cuenta.
    /// </summary>
    public static SesionDto ASesion(
        Usuario usuario, IEnumerable<UsuarioRol> integrantes, IReadOnlyCollection<Guid>? limitacion = null) => new(
        usuario.Id,
        usuario.Correo,
        usuario.EsDesarrollador,
        usuario.VersionFoto,
        integrantes
            .GroupBy(integrante => integrante.ClubId)
            .Select(delClub => AClubDeSesion(
                delClub.OrderBy(integrante => integrante.CreadoEn).ThenBy(integrante => integrante.Id).ToList(),
                limitacion))
            .OfType<ClubDeSesionDto>()
            .ToList());

    /// <summary>
    /// Club del desplegable a partir de un integrante con su club cargado, sin nada que elegir.
    /// </summary>
    public static ClubDeSesionDto AClubDeSesion(UsuarioRol integrante) => AClubDeSesion(integrante, []);

    /// <summary>Jugador de la lista en la que la familia elige con cuál continuar.</summary>
    public static JugadorDeSesionDto AJugadorDeSesion(UsuarioRol integrante) => new(
        integrante.Id,
        integrante.Nombres,
        integrante.Apellidos,
        integrante.EstadoIngreso,
        !integrante.Activo);

    /// <summary>Token de sesión recién emitido.</summary>
    public static TokenSesionDto AToken(TokenEmitido token) => new(token.Token, token.VenceEn);

    /// <summary>
    /// La entrada de un club a partir de los integrantes de la cuenta en él, del más antiguo al
    /// más reciente; nulo si son varios y la sesión está limitada a uno que no es ninguno de ellos.
    /// </summary>
    private static ClubDeSesionDto? AClubDeSesion(List<UsuarioRol> delClub, IReadOnlyCollection<Guid>? limitacion)
    {
        if (delClub.Count == 1)
        {
            return AClubDeSesion(delClub[0]);
        }

        if (limitacion is not null)
        {
            // Una sesión limitada no recibe ni el nombre ni el estado de los hermanos (RF-026).
            var limitado = delClub.FirstOrDefault(integrante => limitacion.Contains(integrante.Id));
            return limitado is null ? null : AClubDeSesion(limitado);
        }

        return AClubDeSesion(delClub[0], delClub.Select(AJugadorDeSesion).ToList());
    }

    private static ClubDeSesionDto AClubDeSesion(UsuarioRol integrante, IReadOnlyList<JugadorDeSesionDto> jugadores)
    {
        var club = integrante.Club
            ?? throw new InvalidOperationException("El integrante debe venir con su club cargado.");

        return new ClubDeSesionDto(
            club.Id,
            club.Nombre,
            integrante.Rol,
            club.Estado,
            integrante.EstadoIngreso,
            !integrante.Activo,
            MapperIdentidadClub.AIdentidad(club),
            integrante.Nombres,
            integrante.Apellidos,
            integrante.Id,
            jugadores);
    }
}
