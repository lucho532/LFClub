using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los comunicados del club de la petición (spec 011).
/// Su responsabilidad es resolver quién recibe un comunicado al publicarlo, guardarlo junto con sus
/// destinos y destinatarios en una sola unidad de trabajo, listarlo para el equipo del club y para
/// la cuenta de un jugador con cursor por identificador, contar los sin leer y marcar la lectura de
/// la cuenta. Trabaja siempre con el filtro de aislamiento activo: nunca ve otro club.
/// No decide quién publica, quién retira ni quién ve qué (eso es de las reglas del dominio), no
/// envía avisos fuera de la aplicación y no borra comunicados ni destinatarios.
/// </summary>
public interface IRepositorioComunicados
{
    /// <summary>
    /// Los jugadores activos del club (rol <c>JUGADOR</c>, ingreso aprobado y sin retirar) que
    /// reciben un comunicado con esa audiencia, en una sola consulta: <c>CLUB</c> todos, tengan o no
    /// categoría; <c>CATEGORIAS</c> los de esas categorías; <c>EQUIPOS</c> los de esos equipos.
    /// </summary>
    Task<IReadOnlyList<DestinatarioDeComunicado>> ResolverDestinatariosAsync(
        AlcanceComunicado alcance,
        IReadOnlyCollection<Guid> categoriaIds,
        IReadOnlyCollection<Guid> equipoIds,
        CancellationToken cancelacion = default);

    /// <summary>De esas categorías, las que existen en el club y están activas.</summary>
    Task<IReadOnlyList<CategoriaDeAudiencia>> CategoriasActivasAsync(
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken cancelacion = default);

    /// <summary>De esos equipos, los que existen en el club y están activos, de una categoría activa.</summary>
    Task<IReadOnlyList<EquipoDeAudiencia>> EquiposActivosAsync(
        IReadOnlyCollection<Guid> equipoIds, CancellationToken cancelacion = default);

    /// <summary>El comunicado con esa clave de publicación, con sus destinos, o nulo.</summary>
    Task<Comunicado?> BuscarPorClaveAsync(Guid clave, CancellationToken cancelacion = default);

    /// <summary>El comunicado con sus destinos, o nulo si no existe en el club.</summary>
    Task<Comunicado?> ObtenerAsync(Guid comunicadoId, CancellationToken cancelacion = default);

    /// <summary>Guarda el comunicado con sus destinos y destinatarios en la misma unidad de trabajo.</summary>
    Task AgregarAsync(Comunicado comunicado, CancellationToken cancelacion = default);

    /// <summary>
    /// Los comunicados del club, del más reciente al más antiguo, con sus destinos: solo los de ese
    /// autor si se indica, y solo los anteriores a <paramref name="antesDe"/> si se indica. Devuelve
    /// hasta <paramref name="limite"/> filas; quien llama pide uno más para saber si hay más.
    /// </summary>
    Task<IReadOnlyList<Comunicado>> ListarPublicadosAsync(
        Guid? autorId, Guid? antesDe, int limite, CancellationToken cancelacion = default);

    /// <summary>Cuántas cuentas distintas recibieron cada comunicado.</summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarFamiliasAsync(
        IReadOnlyCollection<Guid> comunicadoIds, CancellationToken cancelacion = default);

    /// <summary>Retira el comunicado con quien lo retira, su nombre copiado y la hora.</summary>
    Task RetirarAsync(
        Guid comunicadoId, Guid porUsuarioRolId, string porNombre, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Los comunicados que le llegaron a la cuenta, del más reciente al más antiguo, uno por
    /// comunicado aunque llegue por varios hermanos. Solo cuentan filas de jugadores activos, de
    /// comunicados sin retirar y, si la sesión está limitada, de esos jugadores. Devuelve hasta
    /// <paramref name="limite"/> filas, con el cursor <paramref name="antesDe"/>.
    /// </summary>
    Task<IReadOnlyList<RecibidoDeCuenta>> ListarRecibidosAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        Guid? antesDe,
        int limite,
        CancellationToken cancelacion = default);

    /// <summary>Cuántos comunicados distintos tiene la cuenta sin leer, con las mismas condiciones.</summary>
    Task<int> ContarSinLeerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default);

    /// <summary>Un comunicado recibido por la cuenta, o nulo si no le llegó, está retirado o no es del club.</summary>
    Task<RecibidoDeCuenta?> ObtenerRecibidoAsync(
        Guid usuarioId,
        IReadOnlyCollection<Guid>? limitacion,
        Guid comunicadoId,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Pone la hora en todas las filas de la cuenta para ese comunicado donde todavía es nula: la
    /// primera lectura no se mueve.
    /// </summary>
    Task MarcarLeidoAsync(Guid usuarioId, Guid comunicadoId, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Marca como leídos todos los comunicados visibles de la cuenta que siguen sin leer.</summary>
    Task MarcarTodosLeidosAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, DateTime ahoraUtc, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa a un jugador activo que recibe un comunicado al publicarse.
/// Su responsabilidad es llevar el jugador y su cuenta.
/// No lleva datos personales.
/// </summary>
/// <param name="UsuarioRolId">Jugador.</param>
/// <param name="UsuarioId">Cuenta dueña del jugador.</param>
public record DestinatarioDeComunicado(Guid UsuarioRolId, Guid UsuarioId);

/// <summary>
/// Representa una categoría activa que se puede usar como destino.
/// Su responsabilidad es llevar su identificador y su año, que es su nombre.
/// No lleva jugadores ni entrenadores.
/// </summary>
/// <param name="CategoriaId">Categoría.</param>
/// <param name="Anio">Año de nacimiento; es su nombre.</param>
public record CategoriaDeAudiencia(Guid CategoriaId, int Anio);

/// <summary>
/// Representa un equipo activo que se puede usar como destino.
/// Su responsabilidad es llevar el equipo, su nombre y su categoría.
/// No lleva jugadores.
/// </summary>
/// <param name="EquipoId">Equipo.</param>
/// <param name="Nombre">Nombre del equipo.</param>
/// <param name="CategoriaId">Categoría del equipo.</param>
/// <param name="Anio">Año de la categoría; es su nombre.</param>
public record EquipoDeAudiencia(Guid EquipoId, string Nombre, Guid CategoriaId, int Anio);

/// <summary>
/// Representa un comunicado tal como lo ve una cuenta de jugador.
/// Su responsabilidad es llevar el comunicado, si la cuenta lo leyó y cuándo.
/// No lleva destinos ni destinatarios.
/// </summary>
/// <param name="Comunicado">El comunicado, sin destinos.</param>
/// <param name="Leido">Si ninguna fila de la cuenta para ese comunicado está sin leer.</param>
/// <param name="LeidoEn">Cuándo lo leyó; nulo si no lo leyó.</param>
public record RecibidoDeCuenta(Comunicado Comunicado, bool Leido, DateTime? LeidoEn);
