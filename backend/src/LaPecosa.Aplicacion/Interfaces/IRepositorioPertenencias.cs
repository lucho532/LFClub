using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa las consultas que cruzan clubes por necesidad: las de la propia cuenta para iniciar
/// sesión y elegir club, las del número de documento de un jugador que pasa a otra cuenta y el
/// cambio de los datos propios de la cuenta con sesión (constitución §7.1, segunda y cuarta
/// excepción, y la de los datos propios desde la 4.5.1).
/// Su responsabilidad es responder, siempre acotado a una cuenta o a números de documento, a qué
/// clubes pertenece una cuenta, cuáles son sus integrantes en uno de ellos (puede tener varios: los
/// hermanos) y qué integrantes tienen un número, en cualquier club. Un mismo número puede estar en
/// dos cuentas, en clubes distintos (§10).
/// No lista integrantes de un club ni entrega datos de un club a quien no pertenece a él.
/// </summary>
public interface IRepositorioPertenencias
{
    /// <summary>Integrantes de una cuenta, cada uno con su club.</summary>
    Task<IReadOnlyList<UsuarioRol>> ListarDeUsuarioAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Todos los integrantes de una cuenta en un club, cada uno con su club, del más antiguo al
    /// más reciente. Vacía si la cuenta no pertenece a él; más de uno cuando hay hermanos.
    /// </summary>
    Task<IReadOnlyList<UsuarioRol>> ListarDeLaCuentaEnClubAsync(
        Guid usuarioId, Guid clubId, CancellationToken cancelacion = default);

    /// <summary>
    /// Integrante más antiguo de una cuenta en un club, con su club; nulo si no pertenece a él.
    /// Si la cuenta tiene hermanos en el club es el primero que entró.
    /// </summary>
    Task<UsuarioRol?> ObtenerAsync(Guid usuarioId, Guid clubId, CancellationToken cancelacion = default);

    /// <summary>
    /// Una cuenta que tiene ese número de documento ya normalizado; nulo si nadie lo tiene. Con el
    /// número en dos cuentas devuelve una cualquiera, así que solo sirve para saber si el número
    /// tiene dueño. Para decidir entre cuentas, <see cref="ListarPorDocumentosAsync"/>.
    /// </summary>
    Task<Usuario?> ObtenerCuentaPorDocumentoAsync(string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Todos los integrantes que tienen alguno de esos números de documento ya normalizados, en
    /// todos los clubes, en una sola consulta y sin seguirlos. Es la única lectura que cruza clubes
    /// sin estar acotada a una cuenta, y lo está siempre a números de documento (§7.1, cuarta
    /// excepción). Con la colección vacía no consulta.
    /// </summary>
    Task<IReadOnlyList<UsuarioRol>> ListarPorDocumentosAsync(
        IReadOnlyCollection<string> numerosNormalizados, CancellationToken cancelacion = default);

    /// <summary>
    /// Identificadores de los integrantes de esa cuenta que tienen ese número de documento ya
    /// normalizado, en todos sus clubes: uno por club como mucho. Son a los que queda limitada
    /// una sesión iniciada con ese documento.
    /// </summary>
    Task<IReadOnlyList<Guid>> IdsDeLaCuentaConDocumentoAsync(
        Guid usuarioId, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Integrante de ese club que tiene ese número de documento ya normalizado, en cualquier
    /// estado; nulo si nadie lo tiene. Sirve para saber, al agregar un hermano, si el documento
    /// ya es de otro integrante o del mismo hermano que la familia ya agregó.
    /// </summary>
    Task<UsuarioRol?> ObtenerPorDocumentoEnClubAsync(
        Guid clubId, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>Indica si ese número de documento ya normalizado existe en el club.</summary>
    Task<bool> ExisteDocumentoEnClubAsync(Guid clubId, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si ese número de documento ya normalizado es, en ese club, el de un jugador que el
    /// club retiró.
    /// </summary>
    Task<bool> EsDocumentoDeRetiradoAsync(Guid clubId, string numeroDocumento, CancellationToken cancelacion = default);

    /// <summary>
    /// Saca de inmediato a ese integrante de todos los equipos en los que juega, dentro de la
    /// transacción en curso. Se usa cuando deja de ser jugador al aceptar una invitación de
    /// presidente.
    /// </summary>
    Task SacarDeSusEquiposAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>Indica si la cuenta tiene algún integrante en algún club.</summary>
    Task<bool> TieneAlgunaAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// Escribe nombres, apellidos y fecha de nacimiento en todos los integrantes PRESIDENTE,
    /// DIRECTIVO o ENTRENADOR de la cuenta, en todos sus clubes, y el celular en la cuenta (spec 008,
    /// §12.6). No toca sus integrantes JUGADOR. Se guarda con la unidad de trabajo.
    /// </summary>
    Task CambiarDatosPropiosAsync(
        Guid usuarioId,
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        string celular,
        CancellationToken cancelacion = default);

    /// <summary>Añade un integrante nuevo; se guarda con la unidad de trabajo.</summary>
    void Agregar(UsuarioRol integrante);
}
