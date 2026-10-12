using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa el enlace enviado por correo para registrarse en un club (constitución §12.1 y
/// §12.5). La envía el DESARROLLADOR al presidente al crear el club, o el PRESIDENTE desde su club.
/// Su responsabilidad es ligar un club, un rol y un correo a un token de un solo uso, del que solo
/// se guarda el hash, y saber si sigue vigente y en qué estado está. Una vez usada, es el único
/// rastro de cómo entró esa persona: con qué rol y quién la invitó (RF-019).
/// No guarda el token en claro ni crea la cuenta: eso lo hace el registro. Su rol no cambia después
/// de crearla: reenviarla crea otra con el mismo rol.
/// </summary>
public class Invitacion : IPerteneceAClub
{
    /// <summary>Días de vigencia de una invitación (RF-012).</summary>
    public const int DiasDeVigencia = 7;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que invita.</summary>
    public Club? Club { get; set; }

    /// <summary>
    /// Rol con el que entra quien la usa. <c>PRESIDENTE</c> en la que envía el DESARROLLADOR al
    /// crear el club; en una enviada desde el club, el que eligió su PRESIDENTE: <c>JUGADOR</c>,
    /// <c>ENTRENADOR</c> o <c>DIRECTIVO</c> (RF-001). Nunca <c>DESARROLLADOR</c>.
    /// </summary>
    public Rol Rol { get; set; }

    /// <summary>Correo invitado, guardado normalizado. El registro usa siempre este correo.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>SHA-256 del token. Único. El token en claro no se guarda.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Resultado del envío del correo.</summary>
    public EstadoEnvio EstadoEnvio { get; set; } = EstadoEnvio.PENDIENTE;

    /// <summary>
    /// Cuenta que la envió. Puede ser la cuenta de un PRESIDENTE o de un DIRECTIVO del club.
    /// </summary>
    public Guid CreadaPorUsuarioId { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadaEn { get; set; }

    /// <summary>Fecha de vencimiento: la de creación más 7 días.</summary>
    public DateTime VenceEn { get; set; }

    /// <summary>Fecha de uso. Nulo hasta que se usa.</summary>
    public DateTime? UsadaEn { get; set; }

    /// <summary>
    /// Fecha de anulación. Además de "reemplazada por otra", ahora también significa "cancelada
    /// por quien invita". Nulo mientras no ocurre ninguna de las dos.
    /// </summary>
    public DateTime? AnuladaEn { get; set; }

    /// <summary>Vigente: sin usar, sin anular y antes de su vencimiento. No se guarda.</summary>
    public bool EstaVigente(DateTime ahoraUtc) => UsadaEn is null && AnuladaEn is null && ahoraUtc < VenceEn;

    /// <summary>Vencida: ni usada ni anulada, pero ya pasó su vencimiento. No se guarda.</summary>
    public bool EstaVencida(DateTime ahoraUtc) => UsadaEn is null && AnuladaEn is null && ahoraUtc >= VenceEn;

    /// <summary>
    /// Estado de la invitación en ese instante (RF-005). No se guarda. Usada gana a cancelada, y
    /// las dos ganan a vencida.
    /// </summary>
    public EstadoInvitacion EstadoEn(DateTime ahoraUtc)
    {
        if (UsadaEn is not null)
        {
            return EstadoInvitacion.USADA;
        }

        if (AnuladaEn is not null)
        {
            return EstadoInvitacion.CANCELADA;
        }

        return ahoraUtc >= VenceEn ? EstadoInvitacion.VENCIDA : EstadoInvitacion.PENDIENTE;
    }
}
