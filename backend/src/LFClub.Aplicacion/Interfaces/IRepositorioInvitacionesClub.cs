using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las invitaciones que envía un club, para su PRESIDENTE y sus DIRECTIVOS.
/// Su responsabilidad es listar, buscar, anular, añadir y borrar invitaciones dentro del club de
/// la petición, que fijó la autorización en <see cref="IContextoClub"/>.
/// No recibe un identificador de club, no ve las invitaciones de otro club y nunca toca las de
/// presidente, que son del panel del DESARROLLADOR (RF-029). No busca por token ni decide si una
/// invitación está pendiente.
/// </summary>
public interface IRepositorioInvitacionesClub
{
    /// <summary>
    /// Por cada correo, su invitación más reciente, de la más reciente a la más antigua. Una
    /// invitación reemplazada por otra al mismo correo no aparece.
    /// </summary>
    Task<IReadOnlyList<Invitacion>> ListarLaMasRecientePorCorreoAsync(CancellationToken cancelacion = default);

    /// <summary>Busca una invitación del club por su identificador.</summary>
    Task<Invitacion?> ObtenerAsync(Guid invitacionId, CancellationToken cancelacion = default);

    /// <summary>Anula las invitaciones del club sin usar ni anular enviadas a ese correo normalizado.</summary>
    Task AnularPendientesAsync(string correoNormalizado, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Añade una invitación nueva; se guarda con la unidad de trabajo.</summary>
    void Agregar(Invitacion invitacion);

    /// <summary>
    /// Estado de ingreso del integrante del club cuya cuenta tiene ese correo normalizado, o nulo
    /// si nadie del club lo tiene.
    /// </summary>
    Task<EstadoIngreso?> EstadoDeIngresoDelCorreoAsync(string correoNormalizado, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si ese correo normalizado es el de la cuenta de un jugador que el club retiró.
    /// </summary>
    Task<bool> EsDeUnRetiradoAsync(string correoNormalizado, CancellationToken cancelacion = default);

    /// <summary>
    /// Nombre completo, por cuenta, de los integrantes del club que tienen alguna de esas cuentas.
    /// Una cuenta que ya no está en el club no aparece en el resultado.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> NombresDeIntegrantesAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra de inmediato, dentro de la transacción en curso, las invitaciones del club enviadas a
    /// ese correo normalizado, usadas o no (RF-027a).
    /// </summary>
    Task BorrarDelCorreoAsync(string correoNormalizado, CancellationToken cancelacion = default);
}
