using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la invitación del presidente de un club.
/// Su responsabilidad es prepararla al crear el club, enviarla y reenviarla mientras no se haya
/// usado. El correo se intenta enviar después de confirmar la transacción, sin colas; si falla, la
/// invitación queda como fallida y el panel permite reenviarla (research §8).
/// No invita a un presidente a un club que ya existe (RF-024). No guarda el token en claro, no
/// accede al contexto de Entity Framework y no conoce HTTP.
/// </summary>
public class ServicioInvitacionPresidente : IServicioInvitacionPresidente
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioInvitacionesPlataforma _invitaciones;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioCorreo _correo;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioInvitacionPresidente(
        IRepositorioClubesPlataforma clubes,
        IRepositorioInvitacionesPlataforma invitaciones,
        IRepositorioUsuarios usuarios,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioCorreo correo,
        IReloj reloj)
    {
        _clubes = clubes;
        _invitaciones = invitaciones;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _correo = correo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task ComprobarCorreoInvitableAsync(string correoNormalizado, CancellationToken cancelacion = default)
    {
        var desarrollador = await _usuarios.ObtenerDesarrolladorAsync(cancelacion);
        if (desarrollador?.CorreoNormalizado == correoNormalizado)
        {
            throw ErroresDeInvitacion.CorreoDelDesarrollador();
        }
    }

    /// <inheritdoc />
    public async Task<(Invitacion Invitacion, string Token)> PrepararAsync(
        Club club, string correoNormalizado, Guid creadaPorUsuarioId, CancellationToken cancelacion = default)
    {
        var ahora = _reloj.AhoraUtc;

        await _invitaciones.AnularPendientesAsync(club.Id, correoNormalizado, ahora, cancelacion);

        var (invitacion, token) = ConstructorInvitaciones.Nueva(
            club.Id, Rol.PRESIDENTE, correoNormalizado, creadaPorUsuarioId, ahora);
        _invitaciones.Agregar(invitacion);

        return (invitacion, token);
    }

    /// <inheritdoc />
    public async Task EnviarAsync(
        Invitacion invitacion, string nombreClub, string token, CancellationToken cancelacion = default)
    {
        var enviado = await _correo.EnviarInvitacionAsync(invitacion.Correo, nombreClub, invitacion.Rol, token, cancelacion);
        invitacion.EstadoEnvio = enviado ? EstadoEnvio.ENVIADO : EstadoEnvio.FALLIDO;
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<InvitacionDto> ReenviarAsync(
        Guid clubId, Guid invitacionId, ReenviarInvitacionDto? datos, Guid usuarioId, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        var anterior = await _invitaciones.ObtenerAsync(clubId, invitacionId, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        if (anterior.UsadaEn is not null)
        {
            throw ExcepcionDeAplicacion.Conflicto(
                "invitacion_ya_usada", "Esa invitación ya se usó: la persona ya se registró.");
        }

        if (anterior.AnuladaEn is not null)
        {
            // Ya fue reemplazada por otra: para el panel dejó de existir.
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var correo = string.IsNullOrWhiteSpace(datos?.Correo) ? anterior.Correo : ValidarCorreo(datos.Correo);
        await ComprobarCorreoInvitableAsync(correo, cancelacion);

        return await ReemplazarYEnviarAsync(club, correo, usuarioId, anterior, cancelacion);
    }

    private static string ValidarCorreo(string? correo)
    {
        var errores = new ErroresDeValidacion();
        ValidadorCorreo.Obligatorio(errores, "correo", correo);
        errores.LanzarSiHayErrores();
        return NormalizadorTexto.Correo(correo);
    }

    private async Task<InvitacionDto> ReemplazarYEnviarAsync(
        Club club, string correo, Guid usuarioId, Invitacion reemplazada, CancellationToken cancelacion)
    {
        var (invitacion, token) = await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                reemplazada.AnuladaEn = _reloj.AhoraUtc;

                var preparada = await PrepararAsync(club, correo, usuarioId, cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
                return preparada;
            },
            cancelacion);

        await EnviarAsync(invitacion, club.Nombre, token, cancelacion);
        return MapperClubPlataforma.AInvitacion(invitacion, _reloj.AhoraUtc);
    }
}
