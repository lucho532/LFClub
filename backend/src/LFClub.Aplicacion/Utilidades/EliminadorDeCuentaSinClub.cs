using LFClub.Aplicacion.Interfaces;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa la regla "una cuenta que pierde su último integrante se elimina" (RF-019a de la 001 y
/// RF-027a de la 002), compartida por el retiro de un presidente y el rechazo de un ingreso.
/// Su responsabilidad es comprobar si a la cuenta le queda algún club y, si no le queda ninguno,
/// eliminarla. Se llama dentro de la transacción de quien lo usa, después de quitar al integrante.
/// La cuenta DESARROLLADOR nunca se toca. No quita integrantes ni decide cuándo se elimina a
/// alguien de un club.
/// </summary>
public class EliminadorDeCuentaSinClub
{
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EliminadorDeCuentaSinClub(
        IRepositorioPertenencias pertenencias, IRepositorioUsuarios usuarios, IUnidadDeTrabajo unidadDeTrabajo)
    {
        _pertenencias = pertenencias;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <summary>Elimina la cuenta si ya no tiene ningún integrante en ningún club.</summary>
    public async Task EliminarSiQuedoSinClubAsync(Guid usuarioId, CancellationToken cancelacion = default)
    {
        if (await _pertenencias.TieneAlgunaAsync(usuarioId, cancelacion))
        {
            return;
        }

        var cuenta = await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion);
        if (cuenta is { EsDesarrollador: false })
        {
            _usuarios.Eliminar(cuenta);
            await _unidadDeTrabajo.GuardarAsync(cancelacion);
        }
    }
}
