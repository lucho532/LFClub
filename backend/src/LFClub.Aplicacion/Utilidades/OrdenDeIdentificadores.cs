namespace LFClub.Aplicacion.Utilidades;

/// <summary>
/// Representa el orden de los identificadores UUID tal como lo aplica PostgreSQL (spec 012,
/// research §3).
/// Su responsabilidad es comparar dos <see cref="Guid"/> byte a byte, sin signo y en orden de red,
/// para mezclar en memoria filas que la base de datos ya ordenó por <c>Id</c> (comunicados y avisos
/// de la campana). Como los identificadores son UUID v7, ese orden es el de creación.
/// No usa <see cref="Guid.CompareTo(Guid)"/>, que compara los campos de la estructura de .NET y no
/// coincide con el orden de la base de datos.
/// </summary>
public static class OrdenDeIdentificadores
{
    /// <summary>Compara dos identificadores en orden ascendente por bytes de red.</summary>
    public static int Comparar(Guid izquierdo, Guid derecho)
    {
        Span<byte> a = stackalloc byte[16];
        Span<byte> b = stackalloc byte[16];
        izquierdo.TryWriteBytes(a, bigEndian: true, out _);
        derecho.TryWriteBytes(b, bigEndian: true, out _);
        return a.SequenceCompareTo(b);
    }

    /// <summary>Compara dos identificadores en orden descendente: el más reciente primero.</summary>
    public static int CompararDescendente(Guid izquierdo, Guid derecho) => Comparar(derecho, izquierdo);
}
