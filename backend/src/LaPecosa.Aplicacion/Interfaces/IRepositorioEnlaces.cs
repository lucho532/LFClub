namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa la lectura de los integrantes del club de la petición que hace falta para resolver a
/// qué ficha lleva cada nombre de una respuesta (spec 008).
/// Su responsabilidad es leer, en una sola consulta por lista, el rol y el estado de ingreso de
/// hoy de los integrantes pedidos, por su cuenta o por su identificador. Trabaja siempre con el
/// filtro de aislamiento: nunca ve otro club.
/// No decide a qué ficha lleva cada nombre (eso es de <c>ReglaEnlaceDeFicha</c>), no modifica
/// datos y no lee nombres ni datos personales.
/// </summary>
public interface IRepositorioEnlaces
{
    /// <summary>Los integrantes de este club de esas cuentas; una cuenta puede tener varios (hermanos).</summary>
    Task<IReadOnlyList<IntegranteDeEnlace>> IntegrantesDeCuentasAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancelacion = default);

    /// <summary>Los integrantes de este club con esos identificadores; los que no existen no salen.</summary>
    Task<IReadOnlyList<IntegranteDeEnlace>> IntegrantesAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);
}
