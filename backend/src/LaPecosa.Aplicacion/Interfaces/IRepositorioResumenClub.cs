namespace LaPecosa.Aplicacion.Interfaces;

/// <summary>
/// Representa las cuentas del resumen del club de la petición (spec 007, RF-011).
/// Su responsabilidad es contar, en el club fijado por la autorización y con el filtro de
/// aislamiento activo, sus jugadores activos, sus equipos activos de categorías activas, las
/// personas distintas que entrenan (asignadas a una categoría activa o con rol ENTRENADOR) y sus
/// categorías activas.
/// No recibe un identificador de club, no guarda nada y no decide quién puede ver las cifras.
/// </summary>
public interface IRepositorioResumenClub
{
    /// <summary>Las cuatro cifras del club de la petición; todas en cero en un club sin nada.</summary>
    Task<(int Jugadores, int Equipos, int Entrenadores, int Categorias)> ContarAsync(
        CancellationToken cancelacion = default);
}
