using LFClub.Api.Errores;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LFClub.Api.Autorizacion;

/// <summary>
/// Representa la autorización del panel de administración de la plataforma (constitución §8,
/// RF-003).
/// Su responsabilidad es dejar pasar solo a la cuenta DESARROLLADOR: sin sesión responde 401
/// <c>sin_sesion</c>; a cualquier otra cuenta, 403 <c>solo_desarrollador</c>, sin ningún dato.
/// No fija ningún club en el contexto: el panel es la primera excepción de §7.1 y sus repositorios
/// viven en <c>Repositorios/Plataforma/</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SoloDesarrolladorAttribute : Attribute, IAuthorizationFilter
{
    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var usuario = ValidadorSesion.UsuarioDe(context.HttpContext);
        if (usuario is null)
        {
            context.Result = Problema.SinSesion().ComoResultado();
            return;
        }

        if (!usuario.EsDesarrollador)
        {
            context.Result = new Problema(
                403, "solo_desarrollador", "Esta zona es solo para la administración de la plataforma.")
                .ComoResultado();
        }
    }
}
