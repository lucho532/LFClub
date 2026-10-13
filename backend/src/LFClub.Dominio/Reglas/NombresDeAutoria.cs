namespace LFClub.Dominio.Reglas;

/// <summary>
/// Representa los nombres que se escriben en lugar del de una persona cuando ya no existe (§13,
/// spec 014).
/// Su responsabilidad es fijar en un solo lugar el texto con el que quedan los comunicados,
/// resultados y demás registros publicados por una cuenta eliminada.
/// No decide qué registros se reescriben: eso lo hace el repositorio de eliminación de cuentas.
/// </summary>
public static class NombresDeAutoria
{
    /// <summary>Autor de lo que publicó una cuenta que se eliminó.</summary>
    public const string PersonaEliminada = "Persona eliminada";
}
