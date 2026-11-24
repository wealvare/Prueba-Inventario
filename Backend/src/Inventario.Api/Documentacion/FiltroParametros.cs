using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventario.Api.Documentacion;

internal sealed class FiltroParametros : IOperationFilter
{
    private static readonly Dictionary<string, string> Descripciones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "Id del recurso.",
        ["pagina"] = "Número de página, desde 1.",
        ["tamanoPagina"] = "Elementos por página, entre 1 y 100.",
        ["busqueda"] = "Texto contenido en el código, el nombre o la descripción.",
        ["categoria"] = "Categoría exacta.",
        ["estado"] = "Activos (por defecto), Inactivos o Todos.",
        ["precioMinimo"] = "Precio mínimo de alguno de los lotes.",
        ["precioMaximo"] = "Precio máximo de alguno de los lotes."
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parametro in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (Descripciones.TryGetValue(parametro.Name ?? string.Empty, out var descripcion))
                parametro.Description = descripcion;
        }
    }
}
