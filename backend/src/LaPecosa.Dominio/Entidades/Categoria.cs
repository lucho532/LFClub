namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa una categoría de un club: el grupo de jugadores nacidos en un mismo año
/// (constitución §11).
/// Su responsabilidad es guardar el club, el año, si está activa y si alguna vez tuvo jugadores,
/// entrenadores o actividades (entrenamientos o partidos, spec 009), que es lo que decide si se
/// puede borrar o solo desactivar (RF-004a). Una categoría con actividades no se borra. Puede ser
/// destino de un comunicado (<see cref="ComunicadoDestino"/>, spec 011), que conserva su nombre
/// copiado aunque la categoría se borre.
/// No lleva nombre libre, horario, sede ni cupo, no cubre varios años y no guarda un historial de
/// quién pasó por ella (§11.2, §18).
/// </summary>
public class Categoria : IPerteneceAClub
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>
    /// Año de nacimiento. Obligatorio. Año de cuatro cifras no posterior al año en curso (RF-002).
    /// No se puede cambiar (RF-004). Es el nombre de la categoría (RF-001).
    /// </summary>
    public int Anio { get; set; }

    /// <summary>Obligatorio. Nace en verdadero.</summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Obligatorio. Nace en falso; pasa a verdadero la primera vez que entra un jugador o un
    /// entrenador o que se le dirige un entrenamiento o un partido, y no vuelve a falso.
    /// </summary>
    public bool Usada { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadaEn { get; set; }

    /// <summary>Equipos de la categoría, activos e inactivos.</summary>
    public List<Equipo> Equipos { get; set; } = [];

    /// <summary>Asignaciones de entrenadores, activas e inactivas.</summary>
    public List<AsignacionEntrenadorCategoria> Asignaciones { get; set; } = [];
}
