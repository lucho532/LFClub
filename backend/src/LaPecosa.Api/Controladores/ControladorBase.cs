using LaPecosa.Api.Autorizacion;
using LaPecosa.Dominio.Entidades;
using Microsoft.AspNetCore.Mvc;

namespace LaPecosa.Api.Controladores;

/// <summary>
/// Representa la base de los controladores de la API.
/// Su responsabilidad es dar acceso a la cuenta de la sesión, a la limitación de esa sesión y al
/// integrante de la petición, ya comprobados por la autenticación y por los atributos de
/// autorización.
/// No contiene reglas de negocio ni comprueba permisos por su cuenta.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ControladorBase : ControllerBase
{
    /// <summary>Identificador de la cuenta con sesión.</summary>
    protected Guid UsuarioId =>
        ValidadorSesion.UsuarioDe(HttpContext)?.Id
        ?? throw new InvalidOperationException("El endpoint exige sesión y no la hay.");

    /// <summary>
    /// Integrantes a los que está limitada la sesión, porque se inició con el documento de un
    /// jugador; nulo si llega a todos los de la cuenta.
    /// </summary>
    protected IReadOnlyCollection<Guid>? Limitacion => ValidadorSesion.LimitacionDe(HttpContext);

    /// <summary>
    /// Integrante de la cuenta que hace la petición en el club de la ruta: el único que tiene, el
    /// jugador elegido o el de la limitación. Exige <c>[IntegranteDelClub]</c>.
    /// </summary>
    protected UsuarioRol Integrante => IntegranteDelClubAttribute.IntegranteDe(HttpContext);
}
