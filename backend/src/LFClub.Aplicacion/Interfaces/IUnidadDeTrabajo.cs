namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa la unidad de trabajo sobre la base de datos.
/// Su responsabilidad es guardar los cambios pendientes y ejecutar varias operaciones en una sola
/// transacción.
/// No contiene consultas ni reglas de negocio.
/// </summary>
public interface IUnidadDeTrabajo
{
    /// <summary>Guarda los cambios pendientes.</summary>
    Task GuardarAsync(CancellationToken cancelacion = default);

    /// <summary>Ejecuta la operación en una transacción; si falla, no queda ningún cambio.</summary>
    Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken cancelacion = default);

    /// <summary>Ejecuta la operación en una transacción; si falla, no queda ningún cambio.</summary>
    Task EnTransaccionAsync(Func<Task> operacion, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si el error viene de un índice único de la base de datos y, en ese caso, de cuál.
    /// </summary>
    bool EsViolacionDeUnicidad(Exception error, out string? nombreIndice);
}
