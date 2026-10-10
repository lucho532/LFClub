using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Aplicacion.Validadores;
using LaPecosa.Dominio.Entidades;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la foto de perfil.
/// Su responsabilidad es validar la imagen con las mismas reglas que el escudo, guardarla y subir
/// la versión de la foto de la cuenta para que la interfaz sepa que cambió.
/// No confía en la extensión ni en el tipo declarado, no accede al contexto de Entity Framework y
/// no conoce HTTP.
/// </summary>
public class ServicioFotoPerfil : IServicioFotoPerfil
{
    private readonly IRepositorioFotosPerfil _fotos;
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioSesion _sesion;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioFotoPerfil(
        IRepositorioFotosPerfil fotos,
        IRepositorioUsuarios usuarios,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioSesion sesion)
    {
        _fotos = fotos;
        _usuarios = usuarios;
        _unidadDeTrabajo = unidadDeTrabajo;
        _sesion = sesion;
    }

    /// <inheritdoc />
    public async Task<(byte[] Contenido, string TipoContenido)> ObtenerAsync(
        Guid usuarioId, CancellationToken cancelacion = default)
    {
        var foto = await _fotos.ObtenerAsync(usuarioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
        return (foto.Contenido, foto.TipoContenido);
    }

    /// <inheritdoc />
    public async Task<SesionDto> GuardarAsync(
        Guid usuarioId,
        byte[]? contenido,
        IReadOnlyCollection<Guid>? limitacion,
        CancellationToken cancelacion = default)
    {
        var usuario = await CuentaAsync(usuarioId, cancelacion);

        var (tipoContenido, motivo) = ValidadorImagen.Validar(contenido ?? []);
        if (tipoContenido is null)
        {
            throw motivo == MotivoRechazoImagen.DemasiadoGrande
                ? new ExcepcionDeAplicacion("foto_demasiado_grande", 400, "La foto pesa más de 1 MB. Usa una imagen más liviana.")
                : new ExcepcionDeAplicacion("foto_no_es_imagen", 400, "La foto debe ser una imagen PNG, JPEG o WebP.");
        }

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _fotos.GuardarAsync(usuarioId, contenido!, tipoContenido, cancelacion);
                usuario.VersionFoto++;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        return await _sesion.ObtenerAsync(usuarioId, limitacion, cancelacion);
    }

    /// <inheritdoc />
    public async Task QuitarAsync(Guid usuarioId, CancellationToken cancelacion = default)
    {
        var usuario = await CuentaAsync(usuarioId, cancelacion);

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _fotos.QuitarAsync(usuarioId, cancelacion);
                usuario.VersionFoto = 0;
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);
    }

    private async Task<Usuario> CuentaAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion) ?? throw ExcepcionDeAplicacion.NoEncontrado();
}
