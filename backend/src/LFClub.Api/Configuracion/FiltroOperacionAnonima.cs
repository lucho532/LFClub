using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LFClub.Api.Configuracion;

/// <summary>
/// Representa el filtro de Swagger que marca los endpoints anónimos.
/// Su responsabilidad es que la documentación generada diga, como el contrato, qué endpoints no
/// exigen sesión (<c>security: []</c>); todos los demás la exigen.
/// No decide qué endpoint es anónimo: solo refleja el atributo que ya lleva.
/// </summary>
public class FiltroOperacionAnonima : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var esAnonimo = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        if (esAnonimo)
        {
            operation.Security = [];
        }
    }
}
