using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que crea un club con la invitación de su presidente.
/// Su responsabilidad es validar los datos, comprobar que el nombre está libre y que el correo no
/// es el del DESARROLLADOR, y guardar club e invitación en una sola transacción.
/// No envía el correo dentro de la transacción ni accede al contexto de Entity Framework.
/// </summary>
public class ServicioCreacionClub : IServicioCreacionClub
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IServicioInvitacionPresidente _invitaciones;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioCreacionClub(
        IRepositorioClubesPlataforma clubes,
        IServicioInvitacionPresidente invitaciones,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _clubes = clubes;
        _invitaciones = invitaciones;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>Error 409 de nombre de club repetido, compartido con la edición del club.</summary>
    public static ExcepcionDeAplicacion NombreRepetido() =>
        ExcepcionDeAplicacion.Conflicto("nombre_de_club_repetido", "Ya existe un club con ese nombre.");

    /// <inheritdoc />
    public async Task<ClubDetalleDto> CrearAsync(
        CrearClubDto datos, Guid usuarioId, CancellationToken cancelacion = default)
    {
        ValidadorCrearClub.Validar(datos);

        var nombre = NormalizadorTexto.SinEspaciosSobrantes(datos.Nombre);
        var nombreNormalizado = NormalizadorTexto.NombreClub(datos.Nombre);
        var correo = NormalizadorTexto.Correo(datos.CorreoPresidente);

        await _invitaciones.ComprobarCorreoInvitableAsync(correo, cancelacion);
        if (await _clubes.ExisteNombreAsync(nombreNormalizado, null, cancelacion))
        {
            throw NombreRepetido();
        }

        var club = new Club
        {
            Nombre = nombre,
            NombreNormalizado = nombreNormalizado,
            Estado = EstadoClub.ACTIVO,
            CreadoEn = _reloj.AhoraUtc,
            MesInicioCobro = ReglaMesDeCobro.PrimerMesDeCobro(_reloj.AhoraUtc),
        };

        Invitacion invitacion;
        string token;
        try
        {
            (invitacion, token) = await _unidadDeTrabajo.EnTransaccionAsync(
                async () =>
                {
                    _clubes.Agregar(club);
                    var preparada = await _invitaciones.PrepararAsync(club, correo, usuarioId, cancelacion);
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);
                    return preparada;
                },
                cancelacion);
        }
        catch (Exception error) when (_unidadDeTrabajo.EsViolacionDeUnicidad(error, out var indice)
            && indice == IndicesUnicos.NombreDeClub)
        {
            // Dos creaciones simultáneas con el mismo nombre: el índice único decide.
            throw NombreRepetido();
        }

        await _invitaciones.EnviarAsync(invitacion, club.Nombre, token, cancelacion);

        return MapperClubPlataforma.ADetalle(club, [], [invitacion], _reloj.AhoraUtc);
    }
}
