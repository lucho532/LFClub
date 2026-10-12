using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de configurar la identidad visual de un club: sus colores y su escudo
/// (constitución §7.2, RF-008 de la 001 y RF-017 a RF-022 de la 007).
/// Su responsabilidad es guardarlos con las mismas reglas desde dos entradas: la del panel de
/// administración, que recibe el club, para el DESARROLLADOR, y la del propio club, que usa el club
/// de la petición, para su PRESIDENTE. Vale el último cambio, venga de quien venga.
/// No decide quién puede usar cada entrada: eso lo comprueba la autorización. La entrada del club
/// no recibe un identificador de club, así que nadie cambia la identidad de un club ajeno.
/// </summary>
public interface IServicioIdentidadClub
{
    /// <summary>Panel: guarda los colores del club. Formato no válido: 400 <c>datos_invalidos</c>.</summary>
    Task<ClubDetalleDto> ActualizarColoresAsync(
        Guid clubId, ActualizarColoresDto datos, CancellationToken cancelacion = default);

    /// <summary>
    /// Panel: guarda o reemplaza el escudo. Si no es PNG, JPEG ni WebP: 400
    /// <c>escudo_no_es_imagen</c>; si pasa de 1 MB: 400 <c>escudo_demasiado_grande</c>. En ambos
    /// casos se conserva el anterior.
    /// </summary>
    Task<ClubDetalleDto> GuardarEscudoAsync(Guid clubId, byte[]? contenido, CancellationToken cancelacion = default);

    /// <summary>
    /// Propio club: guarda los colores del club de la petición, con las mismas reglas que el panel,
    /// y lo devuelve tal como lo ve <paramref name="quienEdita"/>.
    /// </summary>
    Task<ClubDto> ActualizarColoresDelClubAsync(
        ActualizarColoresDto datos, UsuarioRol quienEdita, CancellationToken cancelacion = default);

    /// <summary>
    /// Propio club: guarda o reemplaza el escudo del club de la petición, con las mismas reglas y
    /// los mismos errores que el panel, y lo devuelve tal como lo ve <paramref name="quienEdita"/>.
    /// </summary>
    Task<ClubDto> ActualizarEscudoDelClubAsync(
        byte[]? contenido, UsuarioRol quienEdita, CancellationToken cancelacion = default);
}
