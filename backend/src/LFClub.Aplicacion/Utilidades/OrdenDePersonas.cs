using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el orden en que se muestran las personas y los equipos en las listas del club.
/// Su responsabilidad es que todas las listas ordenen igual: las personas por apellidos y después
/// por nombres, y los equipos por nombre, sin distinguir mayúsculas.
/// No filtra ni decide quién aparece en cada lista.
/// </summary>
public static class OrdenDePersonas
{
    /// <summary>Comparación de textos para ordenar, sin distinguir mayúsculas.</summary>
    public static readonly StringComparer Comparador = StringComparer.InvariantCultureIgnoreCase;

    /// <summary>Ordena integrantes por apellidos y, a igualdad, por nombres.</summary>
    public static IOrderedEnumerable<UsuarioRol> PorApellidos(this IEnumerable<UsuarioRol> integrantes) =>
        integrantes
            .OrderBy(integrante => integrante.Apellidos, Comparador)
            .ThenBy(integrante => integrante.Nombres, Comparador);
}
