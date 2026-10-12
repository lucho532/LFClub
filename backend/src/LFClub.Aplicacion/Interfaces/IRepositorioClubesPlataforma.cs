using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los clubes desde el panel de administración de la plataforma
/// (constitución §7.1, primera excepción).
/// Su responsabilidad es exponer solo lo que §8 permite al DESARROLLADOR: los clubes y sus
/// presidentes.
/// No expone fichas, datos médicos, finanzas, pagos ni información deportiva de ningún club, ni
/// integrantes que no sean presidentes.
/// </summary>
public interface IRepositorioClubesPlataforma
{
    /// <summary>Todos los clubes, cada uno con la indicación de si ya tiene un presidente registrado.</summary>
    Task<IReadOnlyList<ClubConPresidente>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Busca un club por su identificador.</summary>
    Task<Club?> ObtenerAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Indica si otro club ya usa ese nombre normalizado.</summary>
    Task<bool> ExisteNombreAsync(
        string nombreNormalizado, Guid? exceptoClubId = null, CancellationToken cancelacion = default);

    /// <summary>Presidentes registrados de un club, cada uno con su cuenta.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarPresidentesAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>
    /// Busca un club bloqueando su fila hasta el final de la transacción, para que las reglas que
    /// cuentan sus presidentes no se crucen con otro cambio simultáneo.
    /// </summary>
    Task<Club?> ObtenerBloqueandoAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Busca a un presidente registrado de un club por el identificador de su integrante.</summary>
    Task<UsuarioRol?> ObtenerPresidenteAsync(Guid clubId, Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Número de presidentes registrados de un club; las invitaciones no cuentan.</summary>
    Task<int> ContarPresidentesAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Elimina del club a un integrante; se guarda con la unidad de trabajo.</summary>
    void EliminarIntegrante(UsuarioRol integrante);

    /// <summary>
    /// Guarda el escudo de un club, reemplazando el que hubiera; se guarda con la unidad de trabajo.
    /// </summary>
    Task GuardarEscudoAsync(
        Guid clubId, byte[] contenido, string tipoContenido, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra el club de inmediato, dentro de la transacción en curso: la cascada borra todo lo que
    /// le pertenece, y después se borran las cuentas que se quedaron sin ningún club. Nunca borra
    /// la cuenta DESARROLLADOR ni toca a quien pertenece también a otro club.
    /// </summary>
    Task EliminarConSusCuentasAsync(Guid clubId, CancellationToken cancelacion = default);

    /// <summary>Añade un club nuevo; se guarda con la unidad de trabajo.</summary>
    void Agregar(Club club);
}
