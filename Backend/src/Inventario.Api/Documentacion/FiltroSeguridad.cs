using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventario.Api.Documentacion;

internal sealed class FiltroSeguridad : IOperationFilter
{
    public const string Esquema = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(Esquema, context.Document)] = [] }
        ];
    }
}
