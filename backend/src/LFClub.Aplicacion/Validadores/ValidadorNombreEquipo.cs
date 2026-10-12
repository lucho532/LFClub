using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Validadores;

/// <summary>
/// Representa la validación del nombre de un equipo (RF-023).
/// Su responsabilidad es exigir un nombre obligatorio, sin espacios sobrantes y de 30 caracteres
/// como máximo, y devolverlo junto con su forma normalizada en minúsculas, que es la que decide si
/// dos nombres son el mismo.
/// No comprueba que el nombre esté libre en la categoría: eso lo hacen el servicio y el índice
/// único.
/// </summary>
public static class ValidadorNombreEquipo
{
    /// <summary>
    /// Valida el nombre y lo devuelve limpio y normalizado; si no es válido lanza
    /// <c>datos_invalidos</c> con el error en el campo <c>nombre</c>.
    /// </summary>
    public static NombreDeEquipo Validar(string? nombre)
    {
        var errores = new ErroresDeValidacion();
        var limpio = NormalizadorTexto.SinEspaciosSobrantes(nombre);
        errores.Obligatorio("nombre", limpio, Equipo.LongitudMaximaDelNombre, "El nombre del equipo");
        errores.LanzarSiHayErrores();

        return new NombreDeEquipo(limpio, limpio.ToLowerInvariant());
    }
}

/// <summary>
/// Representa el nombre ya validado de un equipo.
/// Su responsabilidad es llevar el nombre tal como se muestra y su forma normalizada.
/// No indica si el nombre está libre.
/// </summary>
/// <param name="Nombre">Nombre para mostrar, sin espacios sobrantes.</param>
/// <param name="Normalizado">El mismo nombre en minúsculas, para la unicidad.</param>
public readonly record struct NombreDeEquipo(string Nombre, string Normalizado);
