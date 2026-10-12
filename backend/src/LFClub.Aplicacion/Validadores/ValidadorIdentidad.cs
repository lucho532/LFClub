using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación de la identidad de un integrante: nombres, apellidos, tipo y número de
/// documento y fecha de nacimiento (constitución §10 y §12.1).
/// Su responsabilidad es que el registro y la ficha del jugador apliquen exactamente las mismas
/// reglas, con los mismos campos y los mismos mensajes: nombres y apellidos obligatorios, de hasta
/// 80 caracteres; un tipo de documento de la lista; un número obligatorio, de hasta 20 caracteres
/// una vez normalizado; y una fecha de nacimiento obligatoria y no futura.
/// No lanza el error: escribe en los <see cref="ErroresDeValidacion"/> de quien lo llama, que
/// decide qué más valida. No comprueba si el documento ya existe ni exige el responsable.
/// </summary>
public static class ValidadorIdentidad
{
    /// <summary>Longitud máxima de los nombres y de los apellidos.</summary>
    public const int MaximoNombre = 80;

    /// <summary>Longitud máxima del número de documento ya normalizado.</summary>
    public const int MaximoDocumento = 20;

    /// <summary>Exige nombres y apellidos, de hasta 80 caracteres cada uno.</summary>
    public static void NombresYApellidos(ErroresDeValidacion errores, string? nombres, string? apellidos)
    {
        errores.Obligatorio("nombres", nombres, MaximoNombre, "El nombre");
        errores.Obligatorio("apellidos", apellidos, MaximoNombre, "Los apellidos");
    }

    /// <summary>Exige un tipo de documento de la lista y un número de hasta 20 caracteres.</summary>
    public static void Documento(ErroresDeValidacion errores, TipoDocumento? tipoDocumento, string? numeroDocumento)
    {
        if (tipoDocumento is null || !Enum.IsDefined(tipoDocumento.Value))
        {
            errores.Agregar("tipoDocumento", "Elige el tipo de documento.");
        }

        // La longitud se mide ya normalizado: sin espacios ni puntos, que no se guardan.
        var documento = NormalizadorTexto.Documento(numeroDocumento);
        if (documento.Length == 0)
        {
            errores.Agregar("numeroDocumento", "El número de documento es obligatorio.");
        }
        else if (documento.Length > MaximoDocumento)
        {
            errores.Agregar("numeroDocumento", "El número de documento admite como máximo 20 caracteres.");
        }
    }

    /// <summary>
    /// Exige una fecha de nacimiento que no sea posterior a <paramref name="hoy"/>. Devuelve si es
    /// válida, para que quien llama pueda seguir validando lo que depende de ella.
    /// </summary>
    public static bool FechaNacimiento(ErroresDeValidacion errores, DateOnly? fechaNacimiento, DateOnly hoy)
    {
        if (fechaNacimiento is null)
        {
            errores.Agregar("fechaNacimiento", "La fecha de nacimiento es obligatoria.");
            return false;
        }

        if (fechaNacimiento > hoy)
        {
            errores.Agregar("fechaNacimiento", "La fecha de nacimiento no puede ser futura.");
            return false;
        }

        return true;
    }
}
