using System.Text.Json.Nodes;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Productos.Dtos;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventario.Api.Documentacion;

internal sealed class FiltroEjemplos : ISchemaFilter
{
    private static readonly Dictionary<Type, string> Ejemplos = new()
    {
        [typeof(SolicitudInicioSesion)] = """{ "nombreUsuario": "admin", "contrasena": "tu-contrasena" }""",
        [typeof(RespuestaInicioSesion)] = """
            { "token": "eyJhbGciOi...", "expiracion": "2026-01-15T15:00:00Z", "nombreUsuario": "admin", "rol": "Administrador" }
            """,
        [typeof(SolicitudCrearProducto)] = """
            {
              "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas", "categoria": "Periféricos",
              "lotes": [{ "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 159900.50, "cantidad": 20 }]
            }
            """,
        [typeof(SolicitudActualizarProducto)] = """
            {
              "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas", "categoria": "Periféricos",
              "activo": true,
              "lotes": [
                { "id": 1, "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 162000, "cantidad": 15 },
                { "numeroLote": "TEC-100-L2", "fechaIngreso": "2026-02-15", "precio": 165000, "cantidad": 30 }
              ]
            }
            """,
        [typeof(ProductoDto)] = """
            {
              "id": 1, "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas",
              "categoria": "Periféricos", "activo": true,
              "lotes": [{ "id": 1, "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 159900.50, "cantidad": 20 }]
            }
            """,
        [typeof(ProductoResumenDto)] = """
            {
              "id": 1, "codigo": "TEC-100", "nombre": "Teclado compacto", "categoria": "Periféricos", "activo": true,
              "cantidadLotes": 2, "cantidadTotal": 45, "precioMinimo": 162000, "precioMaximo": 165000
            }
            """
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema esquema && Ejemplos.TryGetValue(context.Type, out var ejemplo))
            esquema.Examples = [JsonNode.Parse(ejemplo)!];
    }
}
