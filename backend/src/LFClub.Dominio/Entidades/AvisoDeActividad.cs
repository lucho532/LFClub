using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Entidades;

/// <summary>
/// Representa una línea corta que el sistema escribe en la campana de las familias cuando algo
/// cambia en un entrenamiento o en un partido (constitución §14.4; spec 012, RF-001 a RF-017).
/// Su responsabilidad es guardar el hecho, la actividad a la que se refiere (tipo e identificador,
/// sin foránea: el aviso sobrevive a la actividad), el texto ya redactado y cuándo se generó. De él
/// cuelgan sus destinatarios.
/// No tiene autor, título, audiencia ni retiro, y no se edita: solo desaparece cuando el sistema lo
/// reemplaza por uno más reciente de la misma actividad sin que la cuenta lo haya leído (RF-025). No
/// decide quién lo recibe ni si la actividad sigue disponible para quien lo abre.
/// </summary>
public class AvisoDeActividad : IPerteneceAClub
{
    /// <summary>Longitud máxima del texto.</summary>
    public const int LongitudMaximaDelTexto = 300;

    /// <summary>Clave primaria; v7, así que ordena por creación y sirve de cursor de la campana.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid ClubId { get; set; }

    /// <summary>Qué pasó con la actividad. Obligatorio.</summary>
    public HechoDeActividad Hecho { get; set; }

    /// <summary>Clase de la actividad. Obligatorio; coherente con <see cref="Hecho"/>, lo garantiza el servicio.</summary>
    public TipoDeActividad ActividadTipo { get; set; }

    /// <summary>
    /// Identificador del entrenamiento o del partido del club. Sin foránea: el aviso no impide
    /// borrar la actividad (research §2).
    /// </summary>
    public Guid ActividadId { get; set; }

    /// <summary>Texto plano ya redactado, de 300 caracteres como máximo. Obligatorio.</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que se generó, en UTC.</summary>
    public DateTime GeneradoEn { get; set; }

    /// <summary>Jugadores activos que lo recibieron al generarse, fijos desde entonces.</summary>
    public List<AvisoDeActividadDestinatario> Destinatarios { get; set; } = [];
}
