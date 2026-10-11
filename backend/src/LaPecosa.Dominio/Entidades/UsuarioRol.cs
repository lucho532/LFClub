using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa la pertenencia de una persona a un club: lo que la especificación llama
/// "integrante" (constitución §12.3).
/// Su responsabilidad es guardar el único rol de la persona en ese club, su identidad en él
/// (nombre, documento y fecha de nacimiento), su estado de ingreso y, cuando el club lo aprobó
/// desde la sala de espera, quién lo hizo, cuándo y con qué rol. Quien entra con una invitación
/// nace aprobado, con el rol de la invitación y sin datos de aprobación. De un jugador guarda
/// además su categoría actual y, si quedó retirado, quién lo hizo y cuándo (constitución §11.2
/// y §14.1): no existe una entidad Jugador aparte, y aquí el jugador es el integrante aprobado con
/// el rol JUGADOR. De él cuelga su ficha (<see cref="FichaJugador"/> y
/// <see cref="DocumentoJugador"/>), desde la que se cambia su documento de identidad y se corrige
/// su identidad sobre esta misma fila, sin crear otro integrante (§10). De él cuelga también su foto
/// propia (<see cref="FotoJugador"/>), que se borra al retirarlo y, cuando exista esa operación, al
/// cambiarlo de rol (§14.2).
/// Una cuenta puede tener varios integrantes con rol JUGADOR en el mismo club: son los hermanos
/// (§12.1.2). Cada uno es una fila propia, con su documento, su estado de ingreso, su categoría,
/// su retiro y su ficha; el hermano agregado desde una ficha nace en espera y recuerda desde cuál
/// se agregó.
/// No guarda el correo, la contraseña, el celular ni el responsable, que son de la cuenta, ni los
/// datos de salud o los archivos, que son de la ficha; y nunca lleva el rol DESARROLLADOR.
/// No guarda un historial de estados, de categorías ni de retiros, ni ningún rastro de un rechazo:
/// quien es rechazado se borra.
/// </summary>
public class UsuarioRol : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>Cuenta de la persona.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Cuenta de la persona.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Único rol en el club: PRESIDENTE, DIRECTIVO, ENTRENADOR o JUGADOR.</summary>
    public Rol Rol { get; set; }

    /// <summary>
    /// Estado de ingreso. Quien entra con una invitación, del club o de presidente, nace
    /// <c>APROBADO</c> (RF-008). Nace <c>EN_ESPERA</c> el jugador que la familia agrega desde la
    /// ficha de un hermano, hasta que el PRESIDENTE lo aprueba (constitución §12.1.1).
    /// </summary>
    public EstadoIngreso EstadoIngreso { get; set; }

    /// <summary>
    /// Fecha y hora de la aprobación, en UTC. Nulo mientras está en espera y en quien entró con
    /// una invitación, que no pasa por ninguna aprobación (RF-019). Los cuatro datos de la
    /// aprobación se rellenan juntos y no cambian después.
    /// </summary>
    public DateTime? AprobadoEn { get; set; }

    /// <summary>
    /// Cuenta de quien aprobó. Opcional. Foránea a <see cref="Usuario"/>; pasa a nulo si esa
    /// cuenta se elimina.
    /// </summary>
    public Guid? AprobadoPorUsuarioId { get; set; }

    /// <summary>Nombres y apellidos de quien aprobó, copiados en ese momento (§13).</summary>
    public string? AprobadoPorNombre { get; set; }

    /// <summary>
    /// Rol con el que quedó al aprobarse desde la sala de espera. Desde la funcionalidad 004 es
    /// siempre <c>JUGADOR</c> (RF-016); las aprobaciones anteriores conservan <c>ENTRENADOR</c> o
    /// <c>DIRECTIVO</c> si lo tenían. No cambia aunque después cambie <see cref="Rol"/>.
    /// </summary>
    public Rol? RolDeIngreso { get; set; }

    /// <summary>
    /// Nombres. Obligatorio. Se registran al entrar; los de un jugador solo los corrige después el
    /// PRESIDENTE, desde su ficha, igual que los apellidos y la fecha de nacimiento. Los de un
    /// PRESIDENTE, DIRECTIVO o ENTRENADOR los cambia él mismo desde "Mi perfil", con sus apellidos y
    /// su fecha de nacimiento, en todos sus clubes a la vez (§12.6, spec 008).
    /// </summary>
    public string Nombres { get; set; } = string.Empty;

    /// <summary>Apellidos. Obligatorio.</summary>
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento (§10). El de un jugador lo cambian, junto con el número, su cuenta y el
    /// PRESIDENTE desde la ficha: por ejemplo al pasar de registro civil a tarjeta de identidad.
    /// </summary>
    public TipoDocumento TipoDocumento { get; set; }

    /// <summary>
    /// Número de documento, sin espacios ni puntos y en minúsculas. Único dentro del club, también
    /// frente a los retirados (RF-017). Con él se inicia sesión: al cambiarlo desde la ficha se
    /// entra con el nuevo. Puede estar en integrantes de dos cuentas, en clubes distintos, solo por
    /// el hermano agregado con el documento de un jugador de otra cuenta (§10); entonces abre la
    /// cuenta del jugador activo.
    /// </summary>
    public string NumeroDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de nacimiento. Obligatoria y no futura. Corregir la de un jugador no lo mueve de
    /// categoría; si no tiene ninguna, lo ubica en la activa del año nuevo.
    /// </summary>
    public DateOnly FechaNacimiento { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>
    /// Opcional. Categoría actual del jugador (RF-013). Solo puede tener valor en un jugador del
    /// club (<see cref="EsJugadorDelClub"/>), y la categoría debe ser de su mismo club y estar
    /// activa (RF-007, RF-012).
    /// </summary>
    public Guid? CategoriaId { get; set; }

    /// <summary>Categoría actual del jugador.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>
    /// Obligatorio. Verdadero por defecto, también para las filas que ya existen. Falso significa
    /// retirado del club (§14.1); solo puede ser falso en un JUGADOR aprobado (RF-041). Queda
    /// retirado de tres formas: lo retira el PRESIDENTE, lo retira su propia cuenta, o queda
    /// retirado al aprobarse en otro club un hermano de otra cuenta con su mismo documento.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Opcional. Fecha y hora del retiro, en UTC. Solo tiene valor mientras está retirado. Los
    /// tres datos del retiro se rellenan juntos al retirar y se vacían juntos al reincorporar.
    /// </summary>
    public DateTime? RetiradoEn { get; set; }

    /// <summary>
    /// Opcional. Cuenta de quien lo retiró: la del PRESIDENTE o la del propio jugador. Nula si
    /// quedó retirado al aprobarse un hermano en otro club, que no deja autor. Foránea a
    /// <see cref="Usuario"/>; pasa a nulo si esa cuenta se elimina.
    /// </summary>
    public Guid? RetiradoPorUsuarioId { get; set; }

    /// <summary>
    /// Opcional. Nombres y apellidos de quien lo retiró, copiados en ese momento (§13).
    /// </summary>
    public string? RetiradoPorNombre { get; set; }

    /// <summary>
    /// Opcional. Foránea a <see cref="UsuarioRol"/> (la misma tabla), borrado a nulo. Solo lo
    /// tiene el hermano agregado desde una ficha: es el jugador desde cuya ficha se agregó. Nulo
    /// en quien entró con invitación.
    /// </summary>
    public Guid? AgregadoDesdeUsuarioRolId { get; set; }

    /// <summary>Jugador desde cuya ficha se agregó a este hermano.</summary>
    public UsuarioRol? AgregadoDesde { get; set; }

    /// <summary>Equipos de su categoría en los que juega; puede no haber ninguno (RF-026).</summary>
    public List<JugadorEquipo> Equipos { get; set; } = [];

    /// <summary>
    /// Indica si es un jugador del club: rol JUGADOR, ingreso aprobado y no retirado. Solo ellos
    /// tienen categoría y equipos (RF-012).
    /// </summary>
    public bool EsJugadorDelClub =>
        Rol == Rol.JUGADOR && EstadoIngreso == EstadoIngreso.APROBADO && Activo;
}
