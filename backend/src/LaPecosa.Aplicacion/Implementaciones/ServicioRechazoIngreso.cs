using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que rechaza un ingreso en espera.
/// Su responsabilidad es borrar, en una sola transacción, al integrante (con una sentencia
/// condicionada a que siga en espera, para que entre aprobar y rechazar a la vez valga la primera
/// acción) y, solo si a su cuenta no le queda ningún otro integrante en el club, las invitaciones
/// del club enviadas a su correo; y, si la cuenta se quedó sin ningún club, la cuenta. Es la
/// eliminación física que justifica la constitución §14.
/// Rechazar a un hermano borra a ese jugador y nada más (RF-018 de la 006): la cuenta de la
/// familia sigue teniendo a sus demás jugadores, y la invitación usada con la que entró el primero
/// es el registro de su ingreso y se conserva.
/// No rechaza a un integrante aprobado, no modifica los otros clubes de la persona ni a sus
/// hermanos, no envía
/// correos y no deja registro del rechazo. No comprueba quién rechaza: solo llega aquí el
/// PRESIDENTE. No accede al contexto de Entity Framework ni conoce HTTP.
/// </summary>
public class ServicioRechazoIngreso : IServicioRechazoIngreso
{
    private readonly IRepositorioIngresos _ingresos;
    private readonly IRepositorioInvitacionesClub _invitaciones;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly EliminadorDeCuentaSinClub _eliminador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRechazoIngreso(
        IRepositorioIngresos ingresos,
        IRepositorioInvitacionesClub invitaciones,
        IRepositorioPertenencias pertenencias,
        EliminadorDeCuentaSinClub eliminador,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _ingresos = ingresos;
        _invitaciones = invitaciones;
        _pertenencias = pertenencias;
        _eliminador = eliminador;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public async Task RechazarAsync(Guid usuarioRolId, CancellationToken cancelacion = default)
    {
        var integrante = await _ingresos.ObtenerAsync(usuarioRolId, cancelacion)
            ?? throw ErroresDeIngreso.NoEncontrado();
        if (integrante.EstadoIngreso != EstadoIngreso.EN_ESPERA)
        {
            throw ErroresDeIngreso.YaAprobado();
        }

        var correo = integrante.Usuario?.CorreoNormalizado
            ?? throw new InvalidOperationException("El integrante debe venir con su cuenta cargada.");

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                if (!await _ingresos.BorrarSiSigueEnEsperaAsync(usuarioRolId, cancelacion))
                {
                    // Otra persona se adelantó: si lo aprobó, sigue existiendo; si lo rechazó, ya no.
                    throw await _ingresos.ObtenerAsync(usuarioRolId, cancelacion) is null
                        ? ErroresDeIngreso.NoEncontrado()
                        : ErroresDeIngreso.YaAprobado();
                }

                // Con un hermano aprobado en el club, las invitaciones de la familia se quedan.
                var quedan = await _pertenencias.ListarDeLaCuentaEnClubAsync(
                    integrante.UsuarioId, integrante.ClubId, cancelacion);
                if (quedan.Count == 0)
                {
                    await _invitaciones.BorrarDelCorreoAsync(correo, cancelacion);
                }

                await _eliminador.EliminarSiQuedoSinClubAsync(integrante.UsuarioId, cancelacion);
            },
            cancelacion);
    }
}
