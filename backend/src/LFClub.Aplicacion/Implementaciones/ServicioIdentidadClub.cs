using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Mappers;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la identidad visual de un club.
/// Su responsabilidad es validar y guardar los colores y el escudo, y subir la versión del escudo
/// cada vez que cambia para que su dirección cambie y la caché se renueve. Las dos entradas, la del
/// panel y la del propio club (spec 007), comparten la validación, los códigos de error y el
/// aumento de versión: solo cambia de dónde sale el club y qué se devuelve.
/// No confía en la extensión ni en el tipo declarado del archivo, no comprueba el contraste de los
/// colores (es un aviso de la pantalla), no accede al contexto de Entity Framework y no conoce HTTP.
/// </summary>
public class ServicioIdentidadClub : IServicioIdentidadClub
{
    private readonly IRepositorioClubesPlataforma _clubes;
    private readonly IRepositorioClub _clubPropio;
    private readonly IRepositorioEscudos _escudos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioConsultaClubes _consulta;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioIdentidadClub(
        IRepositorioClubesPlataforma clubes,
        IRepositorioClub clubPropio,
        IRepositorioEscudos escudos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioConsultaClubes consulta)
    {
        _clubes = clubes;
        _clubPropio = clubPropio;
        _escudos = escudos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _consulta = consulta;
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> ActualizarColoresAsync(
        Guid clubId, ActualizarColoresDto datos, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await AplicarColoresAsync(club, datos, cancelacion);
        return await _consulta.ObtenerDetalleAsync(clubId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<ClubDetalleDto> GuardarEscudoAsync(
        Guid clubId, byte[]? contenido, CancellationToken cancelacion = default)
    {
        var club = await _clubes.ObtenerAsync(clubId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await GuardarEscudoAsync(
            club, contenido, tipo => _clubes.GuardarEscudoAsync(clubId, contenido!, tipo, cancelacion), cancelacion);
        return await _consulta.ObtenerDetalleAsync(clubId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<ClubDto> ActualizarColoresDelClubAsync(
        ActualizarColoresDto datos, UsuarioRol quienEdita, CancellationToken cancelacion = default)
    {
        var club = await _clubPropio.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await AplicarColoresAsync(club, datos, cancelacion);
        return MapperClub.AClub(club, quienEdita);
    }

    /// <inheritdoc />
    public async Task<ClubDto> ActualizarEscudoDelClubAsync(
        byte[]? contenido, UsuarioRol quienEdita, CancellationToken cancelacion = default)
    {
        var club = await _clubPropio.ObtenerAsync(cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        await GuardarEscudoAsync(club, contenido, tipo => _escudos.GuardarAsync(contenido!, tipo, cancelacion), cancelacion);
        return MapperClub.AClub(club, quienEdita);
    }

    private async Task AplicarColoresAsync(Club club, ActualizarColoresDto datos, CancellationToken cancelacion)
    {
        ValidadorColores.Validar(datos);

        club.ColorPrincipal = datos.ColorPrincipal!.ToUpperInvariant();
        club.ColorAcento = datos.ColorAcento!.ToUpperInvariant();
        await _unidadDeTrabajo.GuardarAsync(cancelacion);
    }

    /// <summary>
    /// Valida la imagen y, si vale, la guarda con <paramref name="guardar"/> y sube la versión del
    /// escudo en la misma transacción. Si no vale, no toca el escudo anterior.
    /// </summary>
    private async Task GuardarEscudoAsync(
        Club club, byte[]? contenido, Func<string, Task> guardar, CancellationToken cancelacion)
    {
        var (tipoContenido, motivo) = ValidadorImagen.Validar(contenido ?? []);
        if (tipoContenido is null)
        {
            throw motivo == MotivoRechazoImagen.DemasiadoGrande
                ? new ExcepcionDeAplicacion("escudo_demasiado_grande", 400, "El escudo pesa más de 1 MB. Usa una imagen más liviana.")
                : new ExcepcionDeAplicacion("escudo_no_es_imagen", 400, "El escudo debe ser una imagen PNG, JPEG o WebP.");
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await guardar(tipoContenido);
                club.VersionEscudo++;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);
    }
}
