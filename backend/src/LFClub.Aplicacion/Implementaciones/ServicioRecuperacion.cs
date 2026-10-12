using LFClub.Aplicacion.DTOs;
using LFClub.Aplicacion.Interfaces;
using LFClub.Aplicacion.Servicios;
using LFClub.Aplicacion.Utilidades;
using LFClub.Aplicacion.Validadores;
using LFClub.Dominio.Entidades;

namespace LFClub.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de recuperación de contraseña.
/// Su responsabilidad es crear la solicitud guardando solo el hash del token, enviar el correo
/// después de confirmar la transacción y, al confirmar, cambiar la contraseña, desbloquear la
/// cuenta y renovar el sello de seguridad.
/// No guarda el token en claro, no accede al contexto de Entity Framework y no conoce HTTP.
/// </summary>
public class ServicioRecuperacion : IServicioRecuperacion
{
    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioSolicitudesRecuperacion _solicitudes;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioCorreo _correo;
    private readonly IHashContrasena _hash;
    private readonly IReloj _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioRecuperacion(
        IRepositorioUsuarios usuarios,
        IRepositorioSolicitudesRecuperacion solicitudes,
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioCorreo correo,
        IHashContrasena hash,
        IReloj reloj)
    {
        _usuarios = usuarios;
        _solicitudes = solicitudes;
        _unidadDeTrabajo = unidadDeTrabajo;
        _correo = correo;
        _hash = hash;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task PedirAsync(PedirRecuperacionDto datos, CancellationToken cancelacion = default)
    {
        var usuario = await _usuarios.ObtenerPorCorreoAsync(NormalizadorTexto.Correo(datos.Correo), cancelacion);
        if (usuario is null)
        {
            return;
        }

        var (token, hash) = GeneradorTokens.Nuevo();
        var ahora = _reloj.AhoraUtc;

        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                await _solicitudes.AnularPendientesAsync(usuario.Id, ahora, cancelacion);
                _solicitudes.Agregar(new SolicitudRecuperacion
                {
                    UsuarioId = usuario.Id,
                    TokenHash = hash,
                    CreadaEn = ahora,
                    VenceEn = ahora.AddMinutes(SolicitudRecuperacion.MinutosDeVigencia),
                });
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);

        await _correo.EnviarRecuperacionAsync(usuario.Correo, token, cancelacion);
    }

    /// <inheritdoc />
    public async Task ConfirmarAsync(RestablecerContrasenaDto datos, CancellationToken cancelacion = default)
    {
        var ahora = _reloj.AhoraUtc;
        var solicitud = await _solicitudes.ObtenerPorHashAsync(GeneradorTokens.Hash(datos.Token), cancelacion);
        if (solicitud?.Usuario is null || !solicitud.EstaVigente(ahora))
        {
            throw EnlaceNoValido();
        }

        var errores = new ErroresDeValidacion();
        ValidadorContrasena.Validar(errores, "contrasenaNueva", datos.ContrasenaNueva);
        errores.LanzarSiHayErrores();

        var usuario = solicitud.Usuario;
        await _unidadDeTrabajo.EnTransaccionAsync(
            async () =>
            {
                // Marcarla de forma atómica garantiza que el enlace sirve una sola vez.
                if (!await _solicitudes.MarcarUsadaAsync(solicitud.Id, ahora, cancelacion))
                {
                    throw EnlaceNoValido();
                }

                usuario.ContrasenaHash = _hash.Calcular(datos.ContrasenaNueva!);
                usuario.IntentosFallidos = 0;
                usuario.Bloqueada = false;
                usuario.SelloSeguridad = Guid.NewGuid();
                await _unidadDeTrabajo.GuardarAsync(cancelacion);
            },
            cancelacion);
    }

    private static ExcepcionDeAplicacion EnlaceNoValido() => new(
        "enlace_no_valido",
        410,
        "Este enlace ya se usó o caducó. Pide uno nuevo con «Olvidé mi contraseña».");
}
