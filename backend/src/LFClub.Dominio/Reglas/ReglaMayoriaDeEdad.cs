namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de la mayoría de edad (RF-010): es menor quien todavía no ha cumplido 18
/// años el día indicado.
/// Su responsabilidad es responder si una persona es menor de edad un día concreto, contando años
/// cumplidos. Quien nació un 29 de febrero cumple años el 1 de marzo en los años no bisiestos.
/// No decide qué día es hoy ni qué se le exige a un menor: eso lo hace quien la usa.
/// </summary>
public static class ReglaMayoriaDeEdad
{
    /// <summary>Años cumplidos a partir de los cuales una persona es mayor de edad.</summary>
    public const int EdadAdulta = 18;

    /// <summary>Indica si quien nació en esa fecha tiene menos de 18 años cumplidos ese día.</summary>
    public static bool EsMenorDeEdad(DateOnly fechaNacimiento, DateOnly hoy) =>
        AniosCumplidos(fechaNacimiento, hoy) < EdadAdulta;

    private static int AniosCumplidos(DateOnly fechaNacimiento, DateOnly hoy)
    {
        var anios = hoy.Year - fechaNacimiento.Year;
        var aunNoCumple = hoy.Month < fechaNacimiento.Month
            || (hoy.Month == fechaNacimiento.Month && hoy.Day < fechaNacimiento.Day);

        return aunNoCumple ? anios - 1 : anios;
    }
}
