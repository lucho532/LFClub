using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que quita el rol a un presidente.
/// Su responsabilidad es comprobar la regla del último presidente dentro de la misma transacción
/// que el cambio, con la fila del club bloqueada para que dos retiros simultáneos no dejen el club
/// sin presidente, y aplicar la opción elegida. Si lo elimina del club, deja a
/// <see cref="EliminadorDeCuentaSinClub"/> decidir si su cuenta se queda sin ningún club.
/// No accede al contexto de Entity Framework ni conoce HTTP. La eliminación física del integrante
/// la ordena la especificación (§14: aún no tiene historial deportivo ni financiero).
/// </summary>
public class ServicioRetiroPresidente : IServicioRetiroPresidente
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly EliminadorDeCuentaSinClub _eliminador;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaClubes _consulta;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRetiroPresidente(
        IRepositorioClubesPlataforma clubes,
        EliminadorDeCuentaSinClub eliminador,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaClubes consulta)
    {
        _clubes = clubes;
        _eliminador = eliminador;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> RetirarAsync(
        Guid clubId, Guid usuarioRolId, RetirarPresidenteDto datos, CancellationToken cancelacion = default)
    {
        Validar(datos);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                _ = await _clubes.ObtenerBloqueandoAsync(clubId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();
                var presidente = await _clubes.ObtenerPresidenteAsync(clubId, usuarioRolId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                var presidentes = await _clubes.ContarPresidentesAsync(clubId, cancelacion);
                if (!ReglaUltimoPresidente.PuedeDejarElRol(presidente.Rol, presidentes))
                {
                    throw ExcepcionDeAplicacion.Conflicto(
                        "ultimo_presidente",
                        "Es el único presidente del club. Primero debe registrarse otro presidente.");
                }

                if (datos.Accion == AccionRetiroPresidente.ASIGNAR_ROL)
                {
                    presidente.Rol = datos.RolNuevo!.Value;
                    await _unidadDeTrabajo.GuardarAsync(cancelacion);
                    return;
                }

                _clubes.EliminarIntegrante(presidente);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);

                // Una cuenta que pierde su último integrante se elimina (RF-019a).
                await _eliminador.EliminarSiQuedoSinClubAsync(presidente.UsuarioId, cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerDetalleAsync(clubId, cancelacion);
    }

    private static void Validar(RetirarPresidenteDto datos)
    {
        var errores = new ErroresDeValidacion();
        if (datos.Accion is null)
        {
            errores.Agregar("accion", "Elige entre asignarle otro rol o eliminarlo del club.");
        }
        else if (datos.Accion == AccionRetiroPresidente.ASIGNAR_ROL
            && datos.RolNuevo is not (Rol.DIRECTIVO or Rol.ENTRENADOR))
        {
            errores.Agregar("rolNuevo", "Elige el rol nuevo: directivo o entrenador.");
        }

        errores.LanzarSiHayErrores();
    }
}
