using LaPecosa.Aplicacion.Interfaces;
using LaPecosa.Dominio.Entidades;
using LaPecosa.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LaPecosa.Api.Autorizacion;

/// <summary>
/// Representa la comprobación de la sesión en cada petición (research §4).
/// Su responsabilidad es cargar la cuenta del token y rechazarlo si la cuenta ya no existe o si su
/// sello de seguridad cambió (por ejemplo, al cambiar la contraseña), y dejar disponible en la
/// petición a qué integrantes está limitada la sesión, cuando el token lo dice (research §3 de la
/// 006).
/// No comprueba el club ni el rol, ni aplica la limitación: eso lo hacen los atributos de
/// autorización.
/// </summary>
public class ValidadorSesion
{
    private const string ClaveUsuario = "LaPecosa.UsuarioDeLaSesion";
    private const string ClaveLimitacion = "LaPecosa.LimitacionDeLaSesion";

    private readonly IRepositorioUsuarios _usuarios;

    /// <summary>Crea el validador con el acceso a las cuentas.</summary>
    public ValidadorSesion(IRepositorioUsuarios usuarios)
    {
        _usuarios = usuarios;
    }

    /// <summary>Cuenta de la sesión de la petición, o nulo si no hay sesión válida.</summary>
    public static Usuario? UsuarioDe(HttpContext contexto) => contexto.Items[ClaveUsuario] as Usuario;

    /// <summary>
    /// Integrantes a los que está limitada la sesión de la petición, porque se inició con el
    /// documento de un jugador; nulo si el token no trae la limitación.
    /// </summary>
    public static IReadOnlyCollection<Guid>? LimitacionDe(HttpContext contexto) =>
        contexto.Items[ClaveLimitacion] as IReadOnlyCollection<Guid>;

    /// <summary>Valida el token ya verificado contra la cuenta guardada.</summary>
    public async Task ValidarAsync(TokenValidatedContext contexto)
    {
        var sub = contexto.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var sello = contexto.Principal?.FindFirst(OpcionesSesion.ReclamacionSello)?.Value;

        if (!Guid.TryParse(sub, out var usuarioId) || !Guid.TryParse(sello, out var selloDelToken))
        {
            contexto.Fail("Token sin cuenta o sin sello.");
            return;
        }

        var jugadores = new HashSet<Guid>();
        foreach (var reclamacion in contexto.Principal!.FindAll(OpcionesSesion.ReclamacionJugadores))
        {
            if (!Guid.TryParse(reclamacion.Value, out var jugador))
            {
                contexto.Fail("Token con una limitación que no se puede leer.");
                return;
            }

            jugadores.Add(jugador);
        }

        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, contexto.HttpContext.RequestAborted);
        if (usuario is null || usuario.SelloSeguridad != selloDelToken)
        {
            contexto.Fail("La cuenta ya no existe o su sello cambió.");
            return;
        }

        contexto.HttpContext.Items[ClaveUsuario] = usuario;
        if (jugadores.Count > 0)
        {
            contexto.HttpContext.Items[ClaveLimitacion] = jugadores;
        }
    }
}
