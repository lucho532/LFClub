namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el club al que está limitada la petición en curso (constitución §7.1).
/// Su responsabilidad es guardar ese club para que el filtro de aislamiento lo aplique.
/// No comprueba que el usuario pertenezca al club: eso lo hace la autorización antes de fijarlo.
/// </summary>
public interface IContextoClub
{
    /// <summary>Club de la petición, o nulo si no hay ninguno (el filtro no devuelve filas).</summary>
    Guid? ClubId { get; }

    /// <summary>Fija el club de la petición.</summary>
    void Fijar(Guid clubId);
}
