using LaPecosa.Api.Autorizacion;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LaPecosa.Api.Configuracion;

/// <summary>
/// Representa el filtro de Swagger que declara la cabecera del jugador elegido.
/// Su responsabilidad es que la documentación generada diga, como el contrato, que toda operación
/// protegida con <see cref="IntegranteDelClubAttribute"/> admite la cabecera opcional
/// <c>X-Jugador-Elegido</c>, con la que una cuenta con varios jugadores en el club dice con cuál
/// continúa.
/// No lee la cabecera ni decide quién hace la petición: eso es del atributo de autorización.
/// </summary>
public class FiltroJugadorElegido : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var esDeClub = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IntegranteDelClubAttribute>().Any();
        if (!esDeClub)
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IntegranteDelClubAttribute.CabeceraJugadorElegido,
            In = ParameterLocation.Header,
            Required = false,
            Description =
                "El integrante de la cuenta con el que se continúa en este club. Obligatoria solo cuando la " +
                "cuenta tiene varios y la sesión no está limitada a uno.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
        });
    }
}
