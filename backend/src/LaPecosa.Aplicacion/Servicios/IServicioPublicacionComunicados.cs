using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de publicar y retirar comunicados del club (spec 011, historias 1 y 4).
/// Su responsabilidad es publicar un comunicado a una audiencia permitida, con al menos un jugador
/// activo y de forma idempotente por su clave, y retirarlo cuando lo hace quien lo publicó o el
/// PRESIDENTE.
/// No conoce HTTP, no envía avisos fuera de la aplicación (RF-028), no edita ni borra comunicados.
/// </summary>
public interface IServicioPublicacionComunicados
{
    /// <summary>
    /// Publica el comunicado. Con una clave ya usada por el mismo integrante devuelve el que ya existe
    /// y <see cref="ResultadoDePublicacion.Creado"/> en falso; con la de otro, <c>409 clave_en_uso</c>.
    /// </summary>
    Task<ResultadoDePublicacion> PublicarAsync(
        PublicarComunicadoDto datos, UsuarioRol quienPregunta, CancellationToken cancelacion = default);

    /// <summary>
    /// Retira el comunicado. <c>404</c> a quien no lo ve, <c>403 no_retira_comunicado</c> a quien lo
    /// ve y no lo publicó ni es PRESIDENTE, <c>409 comunicado_retirado</c> si ya estaba retirado.
    /// </summary>
    Task<ComunicadoPublicadoDto> RetirarAsync(
        Guid comunicadoId, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}

/// <summary>
/// Representa el resultado de publicar un comunicado.
/// Su responsabilidad es llevar el comunicado y si se creó ahora (201) o ya existía (200).
/// No lleva nada más.
/// </summary>
/// <param name="Comunicado">El comunicado, como lo ve quien lo publicó.</param>
/// <param name="Creado">Verdadero si se creó con esta petición.</param>
public record ResultadoDePublicacion(ComunicadoPublicadoDto Comunicado, bool Creado);
