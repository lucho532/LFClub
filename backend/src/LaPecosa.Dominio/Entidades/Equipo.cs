namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa un equipo: una división con nombre de una categoría ("A", "B", "Élite";
/// constitución §11.3).
/// Su responsabilidad es guardar la categoría a la que pertenece, su nombre, si está activo y si
/// alguna vez tuvo jugadores o entrenadores (RF-024a).
/// Puede ser destino de un comunicado (<see cref="ComunicadoDestino"/>, spec 011), que conserva su
/// nombre copiado aunque el equipo se borre.
/// No pasa a otra categoría (RF-024), no se reactiva una vez desactivado y no guarda un historial
/// de quién jugó en él.
/// </summary>
public class Equipo : IPerteneceAClub
{
    /// <summary>Longitud máxima del nombre (RF-023).</summary>
    public const int LongitudMaximaDelNombre = 30;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>Categoría del equipo. Obligatorio. No se puede cambiar (RF-024).</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Categoría del equipo.</summary>
    public Categoria? Categoria { get; set; }

    /// <summary>Obligatorio, sin espacios sobrantes, 30 caracteres como máximo (RF-023).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary><see cref="Nombre"/> en minúsculas. Solo para la unicidad.</summary>
    public string NombreNormalizado { get; set; } = string.Empty;

    /// <summary>Obligatorio. Nace en verdadero. No existe la reactivación.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Obligatorio. Nace en falso; pasa a verdadero la primera vez que tiene un jugador o un
    /// entrenador.
    /// </summary>
    public bool Usado { get; set; }

    /// <summary>Fecha de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>Jugadores que están hoy en el equipo.</summary>
    public List<JugadorEquipo> Jugadores { get; set; } = [];

    /// <summary>Entrenadores que lo dirigen hoy.</summary>
    public List<EntrenadorEquipo> Entrenadores { get; set; } = [];
}
