using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de publicar dónde se le paga al club (spec 015, RF-026 a RF-028).
/// Su responsabilidad es listar los medios de pago según quien pregunta (el PRESIDENTE y el DIRECTIVO
/// ven todos; la cuenta de un jugador, solo los activos) y crear, editar, desactivar y quitar medios.
/// No comprueba el rol de quien cambia (lo hace la autorización del controlador) y no toca los pagos:
/// cada pago guarda su entidad como texto.
/// </summary>
public interface IServicioMediosPago
{
    /// <summary>Los medios que ve quien pregunta.</summary>
    Task<IReadOnlyList<MedioPagoDto>> ListarAsync(UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>Crea un medio. Datos inválidos: 400.</summary>
    Task<MedioPagoDto> CrearAsync(GuardarMedioPagoDto datos, CancellationToken cancelacion = default);

    /// <summary>Edita o desactiva un medio. Inexistente o de otro club: 404.</summary>
    Task<MedioPagoDto> EditarAsync(Guid medioId, GuardarMedioPagoDto datos, CancellationToken cancelacion = default);

    /// <summary>Quita un medio. Inexistente o de otro club: 404.</summary>
    Task QuitarAsync(Guid medioId, CancellationToken cancelacion = default);
}
