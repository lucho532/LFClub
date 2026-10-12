using LFClub.Aplicacion.DTOs;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa las consultas del apartado "Ingresos" de un club (RF-017 y RF-019).
/// Su responsabilidad es entregar la sala de espera y la lista de ingresos aprobados del club de
/// la petición. Quien entró con una invitación no aparece en ninguna de las dos: se ve en la lista
/// de invitaciones, como invitación usada.
/// No modifica nada ni decide quién puede consultarlas: eso lo comprueba la autorización, que solo
/// admite al PRESIDENTE.
/// </summary>
public interface IServicioConsultaIngresos
{
    /// <summary>Personas en espera del club, de la más antigua a la más reciente.</summary>
    Task<IReadOnlyList<IngresoEnEsperaDto>> ListarEnEsperaAsync(CancellationToken cancelacion = default);

    /// <summary>Ingresos aprobados del club, del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<IngresoAprobadoDto>> ListarAprobadosAsync(CancellationToken cancelacion = default);
}
