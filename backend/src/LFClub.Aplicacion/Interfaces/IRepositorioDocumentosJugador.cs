using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Interfaces;

/// <summary>
/// Representa el acceso a los archivos entregados para los documentos que pide la ficha, dentro
/// del club de la petición.
/// Su responsabilidad es decir qué documentos entregó un jugador, contar los entregados de una
/// lista de jugadores, leer un archivo y guardar el archivo de un documento pedido, que sustituye
/// al que hubiera: como mucho hay uno por jugador y documento (RF-029).
/// No recibe un identificador de club ni ve archivos de otro club. No valida el formato ni decide
/// quién puede abrir o subir un archivo, y solo lee el contenido cuando se pide abrirlo.
/// </summary>
public interface IRepositorioDocumentosJugador
{
    /// <summary>Los documentos que ese jugador tiene entregados, sin leer el contenido de ninguno.</summary>
    Task<IReadOnlyList<DocumentoEntregado>> EstadoAsync(Guid usuarioRolId, CancellationToken cancelacion = default);

    /// <summary>
    /// Cuántos documentos tiene entregados cada uno de esos jugadores, en una sola consulta y sin
    /// leer el contenido de ninguno. Quien no ha entregado nada no aparece en el resultado.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarEntregadosAsync(
        IReadOnlyCollection<Guid> usuarioRolIds, CancellationToken cancelacion = default);

    /// <summary>El archivo de ese documento, con su contenido y sin seguimiento; nulo si está pendiente.</summary>
    Task<DocumentoJugador?> ObtenerAsync(
        Guid usuarioRolId, DocumentoPedido documento, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda de inmediato el archivo de ese documento: inserta la fila o sustituye el contenido,
    /// el tipo, el tamaño y la fecha de la que hay. Debe llamarse con el club ya bloqueado.
    /// </summary>
    Task GuardarAsync(
        UsuarioRol jugador,
        DocumentoPedido documento,
        byte[] contenido,
        string tipoContenido,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default);
}
