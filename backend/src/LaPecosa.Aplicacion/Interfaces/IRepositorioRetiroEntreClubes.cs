namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa la única escritura en un club desde otro club: el retiro del jugador de otra cuenta
/// que tiene el número de documento de un hermano aprobado (constitución §7.1, cuarta excepción, y
/// §14.1; research §13 y §19 de la 006).
/// Su responsabilidad es bloquear varios clubes a la vez, en un orden fijo, y retirar por número de
/// documento a los jugadores aprobados y activos de las demás cuentas, en cualquier club.
/// No decide si se admite el documento ni a quién hay que retirar: eso es de
/// <c>ReglaDocumentoCompartido</c>. No toca la ficha, los documentos, la identidad, el estado de
/// ingreso ni la cuenta del retirado, y nunca devuelve datos del otro club. Sí borra su foto
/// propia, que no se conserva cuando deja el club (§14.2, spec 007).
/// </summary>
public interface IRepositorioRetiroEntreClubes
{
    /// <summary>
    /// Bloquea las filas de esos clubes hasta que termine la transacción en curso, en una sola
    /// sentencia y ordenadas por identificador, para que dos operaciones que bloquean los mismos
    /// clubes en orden distinto no se esperen la una a la otra. Es el mismo recurso que bloquea
    /// <see cref="IRepositorioClub.BloquearAsync"/>.
    /// </summary>
    Task BloquearClubesAsync(IReadOnlyCollection<Guid> clubIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Retira a todo integrante con ese número de documento ya normalizado, de una cuenta distinta
    /// de <paramref name="cuentaQueQueda"/>, con rol JUGADOR, aprobado y activo, en cualquier club:
    /// lo saca de sus equipos, borra su foto, lo deja inactivo y sin categoría, con
    /// <paramref name="retiradoEn"/> y sin autor (supuesto 6 de la 006). Devuelve cuántos retiró;
    /// cero si no había ninguno.
    /// </summary>
    Task<int> RetirarDeOtrasCuentasAsync(
        string numeroDocumento, Guid cuentaQueQueda, DateTime retiradoEn, CancellationToken cancelacion = default);
}
