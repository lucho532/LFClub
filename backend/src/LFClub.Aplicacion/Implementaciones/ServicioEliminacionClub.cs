using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Reglas;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que elimina un club.
/// Su responsabilidad es comprobar, en una sola transacción y con la fila del club bloqueada, que
/// el club está dado de baja y que el nombre de confirmación coincide, y entonces borrarlo.
/// No borra tabla por tabla: la cascada desde el club garantiza que no queda rastro aunque
/// funcionalidades futuras añadan tablas (research §12).
/// </summary>
public class ServicioEliminacionClub : IServicioEliminacionClub
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioEliminacionClub(IRepositorioClubesPlataforma clubes, IUnidadDeTrabajo unidadDeTrabajo)
    {
        _clubes = clubes;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public Task EliminarAsync(Guid clubId, EliminarClubDto datos, CancellationToken cancelacion = default) =>
        _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                var club = await _clubes.ObtenerBloqueandoAsync(clubId, cancelacion)
                    ?? throw ExcepcionDeAplicacion.NoEncontrado();

                if (!ReglaTransicionEstadoClub.SePuedeEliminar(club.Estado))
                {
                    throw ExcepcionDeAplicacion.Conflicto(
                        "club_no_dado_de_baja", "Solo se puede eliminar un club que ya está dado de baja.");
                }

                if (!string.Equals(datos.NombreDeConfirmacion?.Trim(), club.Nombre, StringComparison.Ordinal))
                {
                    throw new ExcepcionDeAplicacion(
                        "confirmacion_no_coincide",
                        400,
                        "El nombre que escribiste no coincide con el del club. No se eliminó nada.");
                }

                await _clubes.EliminarConSusCuentasAsync(clubId, cancelacion);
            },
            cancelacion);
}
