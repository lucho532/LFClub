using LaPecosa.Aplicacion.DTOs;
using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Aplicacion.Mappers;
using LaPecosa.Aplicacion.Servicios;
using LaPecosa.Aplicacion.Utilidades;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Dominio.Reglas;

namespace LaPecosa.Aplicacion.Implementaciones;

/// <summary>
/// Representa el servicio de la sesión.
/// Su responsabilidad es comprobar las credenciales sin revelar si una cuenta existe (RF-005),
/// contar los fallos seguidos hasta bloquear la cuenta al quinto, emitir y renovar tokens y listar
/// los clubes de la cuenta. Un documento puede estar en varias cuentas (el hermano de la 006): abre
/// la del jugador activo y solo admite su contraseña (research §15 de la 006). Si se entra con un
/// documento, emite el token limitado a los integrantes de esa cuenta que tienen ese número, uno
/// por club, y lo conserva al renovar: se guardan sus identificadores y no el número, para que
/// cambiar el documento del jugador durante la sesión no la rompa (research §3 de la 006).
/// No accede al contexto de Entity Framework ni conoce HTTP, y no normaliza la contraseña. No
/// aplica la limitación a las peticiones: eso es de la autorización.
/// </summary>
public class ServicioSesion : IServicioSesion
{
    private const string MensajeCredenciales =
        "El correo, el documento o la contraseña no son correctos. Si no recuerdas tu contraseña o tu " +
        "cuenta quedó bloqueada, usa «Olvidé mi contraseña» para crear una nueva.";

    private readonly IRepositorioUsuarios _usuarios;
    private readonly IRepositorioPertenencias _pertenencias;
    private readonly IHashContrasena _hash;
    private readonly IEmisorTokenSesion _emisor;
    private readonly Lazy<string> _hashDeRelleno;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ServicioSesion(
        IRepositorioUsuarios usuarios,
        IRepositorioPertenencias pertenencias,
        IHashContrasena hash,
        IEmisorTokenSesion emisor)
    {
        _usuarios = usuarios;
        _pertenencias = pertenencias;
        _hash = hash;
        _emisor = emisor;
        _hashDeRelleno = new Lazy<string>(() => hash.Calcular(Guid.NewGuid().ToString()));
    }

    /// <inheritdoc />
    public async Task<TokenSesionDto> IniciarAsync(IniciarSesionDto datos, CancellationToken cancelacion = default)
    {
        var identificador = NormalizadorTexto.Identificador(datos.Identificador);
        var contrasena = datos.Contrasena ?? string.Empty;

        var usuario = await BuscarCuentaAsync(identificador, cancelacion);

        // Aunque la cuenta no exista o no pueda entrar se calcula un hash, para que el tiempo de
        // respuesta no la delate (research §5).
        var hashGuardado = usuario?.ContrasenaHash ?? _hashDeRelleno.Value;
        var coincide = _hash.Verificar(hashGuardado, contrasena);

        if (usuario is null || !usuario.PuedeIniciarSesion || !coincide)
        {
            // Solo cuenta el fallo de una cuenta que podía entrar; una bloqueada no admite ni la
            // contraseña correcta, y la respuesta es siempre la misma (RF-005).
            if (usuario is { PuedeIniciarSesion: true })
            {
                await _usuarios.RegistrarFalloDeSesionAsync(usuario.Id, cancelacion);
            }

            throw new ExcepcionDeAplicacion("credenciales_invalidas", 401, MensajeCredenciales);
        }

        // Un acierto antes del quinto fallo reinicia el contador.
        if (usuario.IntentosFallidos > 0)
        {
            await _usuarios.ReiniciarFallosDeSesionAsync(usuario.Id, cancelacion);
        }

        // Con el documento de un jugador la sesión queda limitada a él; con el correo, no (RF-026).
        var jugadores = identificador.Contains('@')
            ? null
            : await _pertenencias.IdsDeLaCuentaConDocumentoAsync(
                usuario.Id, NormalizadorTexto.Documento(identificador), cancelacion);

        return MapperSesion.AToken(_emisor.Emitir(usuario.Id, usuario.SelloSeguridad, jugadores));
    }

    /// <inheritdoc />
    public async Task<SesionDto> ObtenerAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default)
    {
        var usuario = await CuentaConSesionAsync(usuarioId, cancelacion);
        var integrantes = await _pertenencias.ListarDeUsuarioAsync(usuarioId, cancelacion);
        return MapperSesion.ASesion(usuario, integrantes, limitacion);
    }

    /// <inheritdoc />
    public async Task<TokenSesionDto> RenovarAsync(
        Guid usuarioId, IReadOnlyCollection<Guid>? limitacion, CancellationToken cancelacion = default)
    {
        var usuario = await CuentaConSesionAsync(usuarioId, cancelacion);
        return MapperSesion.AToken(_emisor.Emitir(usuario.Id, usuario.SelloSeguridad, limitacion));
    }

    private Task<Usuario?> BuscarCuentaAsync(string identificador, CancellationToken cancelacion)
    {
        if (identificador.Length == 0)
        {
            return Task.FromResult<Usuario?>(null);
        }

        // Con arroba es un correo; sin ella, un número de documento.
        return identificador.Contains('@')
            ? _usuarios.ObtenerPorCorreoAsync(identificador, cancelacion)
            : CuentaDelDocumentoAsync(NormalizadorTexto.Documento(identificador), cancelacion);
    }

    /// <summary>
    /// La cuenta que abre ese número: si está en varias, la del jugador activo, luego la del que
    /// espera y luego la del retirado (RF-040 de la 006). La contraseña solo se compara con esa.
    /// </summary>
    private async Task<Usuario?> CuentaDelDocumentoAsync(string numero, CancellationToken cancelacion)
    {
        var integrantes = await _pertenencias.ListarPorDocumentosAsync([numero], cancelacion);
        var cuenta = ReglaDocumentoCompartido.CuentaQueAbre(integrantes);
        return cuenta is null ? null : await _usuarios.ObtenerPorIdAsync(cuenta.Value, cancelacion);
    }

    private async Task<Usuario> CuentaConSesionAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await _usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
        ?? throw new ExcepcionDeAplicacion("sin_sesion", 401, "Tu sesión no es válida o terminó. Inicia sesión de nuevo.");
}
