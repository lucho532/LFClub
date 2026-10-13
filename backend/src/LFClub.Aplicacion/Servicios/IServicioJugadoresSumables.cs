using LFClub.Aplicacion.DTOs;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Servicios;

/// <summary>
/// Representa el caso de uso de buscar jugadores que se pueden sumar a un entrenamiento o a un
/// partido (spec 013, RF-013 y RF-016; research §9).
/// Su responsabilidad es devolver, a quien dirige, los jugadores activos del club con categoría que
/// coinciden con el texto, con lo mínimo para elegirlos.
/// No da acceso a la ficha de nadie ni devuelve foto, documento ni equipos.
/// </summary>
public interface IServicioJugadoresSumables
{
    /// <summary>
    /// Hasta 20 coincidencias por apellidos. Con menos de 2 letras, <c>400 datos_invalidos</c>. Quien
    /// no tiene ninguna categoría asignada y no es el PRESIDENTE recibe una lista vacía.
    /// </summary>
    Task<IReadOnlyList<JugadorSumableDto>> BuscarAsync(
        string? busqueda, UsuarioRol quienPregunta, CancellationToken cancelacion = default);
}
