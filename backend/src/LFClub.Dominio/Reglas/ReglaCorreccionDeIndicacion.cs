using LFClub.Dominio.Enumeraciones;

namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa la regla de cuándo quien entrena o el PRESIDENTE corrigen lo de un convocado al
/// guardar el resultado (spec 010, RF-012; research §2, supuesto 1).
/// Su responsabilidad es decidir el origen nuevo de una fila a partir de cómo estaba y cómo queda:
/// pasa a <see cref="OrigenResultado.GESTION"/> si cambia si jugó o sus goles, o si queda como que
/// jugó sin que nadie lo hubiera indicado; si queda igual, conserva su origen (la de una familia
/// sigue siendo suya y la que nadie indicó sigue sin indicar).
/// No consulta la base de datos ni guarda nada, y no se usa cuando escribe la familia.
/// </summary>
public static class ReglaCorreccionDeIndicacion
{
    /// <summary>El origen de la fila después de guardar quien gestiona.</summary>
    public static OrigenResultado? OrigenNuevo(
        OrigenResultado? origenAntes, bool jugoAntes, int golesAntes, bool jugoDespues, int golesDespues)
    {
        if (jugoAntes != jugoDespues || golesAntes != golesDespues)
        {
            return OrigenResultado.GESTION;
        }

        return origenAntes is null && jugoDespues ? OrigenResultado.GESTION : origenAntes;
    }
}
