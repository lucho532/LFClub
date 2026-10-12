using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio que consulta la ficha de un jugador.
/// Su responsabilidad es resolver el acceso con <see cref="AccesoAFicha"/>, leer la ficha y, solo
/// para quien puede verlos, el estado de sus documentos, la fecha de su foto (spec 007) y el
/// enlace a la ficha básica de quien la cambió por última vez (spec 008), y convertirlo todo en el
/// DTO con el mapper, que omite lo que el alcance no permite. Todas las
/// operaciones de la ficha devuelven lo que arma este servicio.
/// No modifica datos, no decide quién ve qué (eso es de la regla), no lee el contenido de ningún
/// archivo ni los bytes de la foto y no accede al contexto de Entity Framework.
/// </summary>
public class ServicioConsultaFicha : IServicioConsultaFicha
{
    private readonly AccesoAFicha _acceso;
    private readonly IRepositorioFichas _fichas;
    private readonly IRepositorioDocumentosJugador _documentos;
    private readonly IRepositorioFotosJugador _fotos;
    private readonly ResolutorDeEnlaces _enlaces;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioConsultaFicha(
        AccesoAFicha acceso,
        IRepositorioFichas fichas,
        IRepositorioDocumentosJugador documentos,
        IRepositorioFotosJugador fotos,
        ResolutorDeEnlaces enlaces,
        IReloj reloj)
    {
        _acceso = acceso;
        _fichas = fichas;
        _documentos = documentos;
        _fotos = fotos;
        _enlaces = enlaces;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<FichaJugadorDto> ObtenerAsync(
        Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken cancelacion = default)
    {
        var (jugador, alcance) = await _acceso.ResolverAsync(usuarioRolId, quienPregunta, cancelacion);
        var ficha = await _fichas.ObtenerAsync(usuarioRolId, cancelacion);

        // A quien no puede ver los documentos ni siquiera se le consulta su estado (RF-007).
        IReadOnlyList<DocumentoEntregado> entregados = alcance.VeDocumentos
            ? await _documentos.EstadoAsync(usuarioRolId, cancelacion)
            : [];

        var fotoActualizadaEn = await _fotos.FechaAsync(usuarioRolId, cancelacion);

        // Un cambio de la familia, o de una cuenta ya eliminada, no lleva enlace: no se consulta.
        EnlaceFichaDto? autorFicha = null;
        if (ficha?.UltimoCambioPorUsuarioId is { } autorId && autorId != jugador.UsuarioId)
        {
            var autores = await _enlaces.AutoresAsync([autorId], cancelacion);
            autorFicha = autores.GetValueOrDefault(autorId);
        }

        return MapperFicha.AFicha(
            jugador, ficha, entregados, alcance, DateOnly.FromDateTime(_reloj.AhoraUtc), fotoActualizadaEn, autorFicha);
    }
}
