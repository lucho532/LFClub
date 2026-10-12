using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.DTOs;

/// <summary>
/// Representa la actividad a la que se refiere un aviso, vista por la cuenta que lo recibió (spec
/// 012, RF-017 y RF-021).
/// Su responsabilidad es llevar el tipo y el identificador para navegar, y si la cuenta todavía puede
/// ver la actividad, calculado en el servidor con las reglas de visibilidad de la 009.
/// No lleva ningún otro dato de la actividad: ni fecha, ni lugar, ni nombres.
/// </summary>
/// <param name="Tipo">Si es un entrenamiento o un partido.</param>
/// <param name="Id">Identificador de la actividad.</param>
/// <param name="Disponible">Si la cuenta puede abrirla hoy; si es falso, la pantalla no navega.</param>
public record ActividadDeAvisoDto(TipoDeActividad Tipo, Guid Id, bool Disponible);
