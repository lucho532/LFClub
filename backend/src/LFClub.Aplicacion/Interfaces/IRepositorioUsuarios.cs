using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a las cuentas.
/// Su responsabilidad es buscar, añadir y eliminar cuentas. La cuenta es una entidad de la
/// plataforma, sin club, y cada consulta devuelve como mucho una.
/// No contiene reglas de negocio ni devuelve los integrantes de la cuenta.
/// </summary>
public interface IRepositorioUsuarios
{
    /// <summary>Busca una cuenta por su identificador.</summary>
    Task<Usuario?> ObtenerPorIdAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>
    /// La cuenta, con seguimiento y recién leída de la base de datos aunque ya estuviera cargada en
    /// la petición, para cambiarla dentro de una transacción sin partir de datos anteriores a ella.
    /// </summary>
    Task<Usuario?> ObtenerParaCambiarAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>Busca una cuenta por su correo ya normalizado.</summary>
    Task<Usuario?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancelacion = default);

    /// <summary>Devuelve la cuenta DESARROLLADOR, si existe.</summary>
    Task<Usuario?> ObtenerDesarrolladorAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Suma un fallo de inicio de sesión de forma atómica en la base de datos y bloquea la cuenta
    /// al llegar al quinto (RF-005). Dos fallos simultáneos cuentan como dos.
    /// </summary>
    Task RegistrarFalloDeSesionAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>Pone a cero los fallos de una cuenta que no está bloqueada.</summary>
    Task ReiniciarFallosDeSesionAsync(Guid usuarioId, CancellationToken cancelacion = default);

    /// <summary>Añade una cuenta nueva; se guarda con la unidad de trabajo.</summary>
    void Agregar(Usuario usuario);

    /// <summary>Elimina una cuenta; se guarda con la unidad de trabajo.</summary>
    void Eliminar(Usuario usuario);
}
