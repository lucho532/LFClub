using LaPecosa.Dominio.Enumeraciones;

namespace LaPecosa.Dominio.Entidades;

/// <summary>
/// Representa un entrenamiento programado para una o varias categorías de un club (constitución
/// §11.1; spec 009, RF-001 a RF-006).
/// Su responsabilidad es guardar la fecha, las horas y el lugar tal como los escribió quien lo
/// programó, en hora de Colombia, su nota, su estado actual y quién lo creó. De él cuelgan sus
/// categorías, las respuestas de las familias y la asistencia registrada.
/// No decide quién lo ve ni quién lo gestiona (eso es de las reglas del dominio), no guarda
/// historial de cambios (§18) y nunca se borra: se cancela (RF-035).
/// </summary>
public class Entrenamiento : IPerteneceAClub
{
    /// <summary>Longitud máxima del lugar.</summary>
    public const int LongitudMaximaDelLugar = 120;

    /// <summary>Longitud máxima de la nota.</summary>
    public const int LongitudMaximaDeLaNota = 500;

    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Club al que pertenece.</summary>
    public Club? Club { get; set; }

    /// <summary>Fecha del entrenamiento, en hora de Colombia. Obligatoria.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora de inicio, en hora de Colombia. Obligatoria.</summary>
    public TimeOnly HoraInicio { get; set; }

    /// <summary>Hora de fin, en hora de Colombia. Obligatoria y mayor que <see cref="HoraInicio"/>.</summary>
    public TimeOnly HoraFin { get; set; }

    /// <summary>Lugar. Obligatorio, de 120 caracteres como máximo y sin espacios a los lados.</summary>
    public string Lugar { get; set; } = string.Empty;

    /// <summary>Nota para las familias. Opcional, de 500 caracteres como máximo.</summary>
    public string? Nota { get; set; }

    /// <summary>Estado actual. Nace <see cref="EstadoEntrenamiento.PROGRAMADO"/>.</summary>
    public EstadoEntrenamiento Estado { get; set; } = EstadoEntrenamiento.PROGRAMADO;

    /// <summary>Fecha y hora de creación, en UTC.</summary>
    public DateTime CreadoEn { get; set; }

    /// <summary>
    /// Integrante que lo programó. Foránea a <see cref="UsuarioRol"/>; pasa a nulo si ese
    /// integrante se borra.
    /// </summary>
    public Guid? CreadoPorUsuarioRolId { get; set; }

    /// <summary>Categorías a las que va dirigido; una o más, y no cambian después de crearlo.</summary>
    public List<EntrenamientoCategoria> Categorias { get; set; } = [];
}
