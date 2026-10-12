namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla del año de una categoría (constitución §11; RF-002).
/// Su responsabilidad es responder si un año sirve para crear una categoría: un año de cuatro
/// cifras no posterior al año en curso.
/// No sabe cuál es el año en curso (lo recibe de quien la llama, que lo saca del reloj) ni comprueba
/// que el club no tenga ya la categoría de ese año.
/// </summary>
public static class ReglaAnioDeCategoria
{
    /// <summary>Primer año de cuatro cifras.</summary>
    public const int AnioMinimo = 1000;

    /// <summary>Indica si el año es válido para una categoría creada en ese año en curso.</summary>
    public static bool EsValido(int anio, int anioEnCurso) => anio >= AnioMinimo && anio <= anioEnCurso;
}
