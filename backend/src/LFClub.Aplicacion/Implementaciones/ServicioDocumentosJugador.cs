using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;
using LFClub.Dominio.Enumeraciones;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de los archivos de la ficha de un jugador.
/// Su responsabilidad es resolver el acceso antes de leer o escribir nada; al abrir, exigir que
/// quien pregunta pueda ver los documentos; y al subir, validar el archivo por su contenido antes
/// de abrir la transacción y, con el club bloqueado, guardarlo sustituyendo al anterior y sellar
/// el último cambio. Un archivo rechazado no toca el que había (RF-030).
/// No confía en la extensión ni en el tipo declarado, no guarda el nombre original del archivo, no
/// escribe su contenido en el registro, no accede al contexto de Entity Framework y no conoce
/// HTTP.
/// </summary>
public class ServicioDocumentosJugador : IServicioDocumentosJugador
{
    private readonly AccesoAFicha _acceso;
    private readonly IRepositorioClub _club;
    private readonly IRepositorioDocumentosJugador _documentos;
    private readonly IRepositorioFichas _fichas;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaFicha _consulta;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioDocumentosJugador(
        AccesoAFicha acceso,
        IRepositorioClub club,
        IRepositorioDocumentosJugador documentos,
        IRepositorioFichas fichas,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaFicha consulta,
        IReloj reloj)
    {
        _acceso = acceso;
        _club = club;
        _documentos = documentos;
        _fichas = fichas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido, string NombreDeArchivo)> AbrirAsync(
        Guid usuarioRolId, string? documento, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var (_, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);
        if (!alcance.VeDocumentos)
        {
            throw ExcepcionDeAplicacion.NoEncontrado();
        }

        var pedido = Pedido(documento);
        var archivo = await _documentos.ObtenerAsync(usuarioRolId, pedido, cancelacion)
            ?? throw ExcepcionDeAplicacion.NoEncontrado();

        return (archivo.Contenido, archivo.TipoContenido, NombreDeArchivo(pedido, archivo.TipoContenido));
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> SubirAsync(
        Guid usuarioRolId,
        string? documento,
        byte[]? contenido,
        UsuarioRol quienSube,
        CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienSube, cancelacion);
        AccesoAFicha.ExigirCambio(alcance);
        var pedido = Pedido(documento);

        var (tipoContenido, motivo) = ValidadorArchivoDeFicha.Validar(contenido ?? []);
        if (tipoContenido is null)
        {
            throw motivo == MotivoRechazoArchivo.DemasiadoGrande
                ? ErroresDeFicha.ArchivoDemasiadoGrande()
                : ErroresDeFicha.ArchivoNoAdmitido();
        }

        // El archivo ya está en memoria: el bloqueo del club no espera a la red (research §10).
        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _club.BloquearAsync(cancelacion);
                var ahora = _reloj.AhoraUtc;
                await _documentos.GuardarAsync(jugador, pedido, contenido!, tipoContenido, ahora, cancelacion);
                await _fichas.SellarCambioAsync(jugador, quienSube, ahora, cancelacion);
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _consulta.ObtenerAsync(usuarioRolId, quienSube, cancelacion);
    }

    /// <summary>
    /// El documento pedido que nombra ese texto de la ruta. Lo que no es exactamente un nombre de
    /// la lista, también un número, responde 404.
    /// </summary>
    private static DocumentoPedido Pedido(string? documento) =>
        Enum.TryParse<DocumentoPedido>(documento, out var pedido) && pedido.ToString() == documento
            ? pedido
            : throw ExcepcionDeAplicacion.NoEncontrado();

    /// <summary>Nombre fijo con el que se sirve el archivo: nunca el que envió la familia.</summary>
    private static string NombreDeArchivo(DocumentoPedido documento, string tipoContenido)
    {
        var nombre = documento switch
        {
            DocumentoPedido.COPIA_DOCUMENTO_IDENTIDAD => "copia-documento-identidad",
            DocumentoPedido.CERTIFICADO_SALUD => "certificado-salud",
            _ => "documento",
        };
        var extension = tipoContenido switch
        {
            ValidadorArchivoDeFicha.TipoPdf => "pdf",
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "bin",
        };

        return $"{nombre}.{extension}";
    }
}
