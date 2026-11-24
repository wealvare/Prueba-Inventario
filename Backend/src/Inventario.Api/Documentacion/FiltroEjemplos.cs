using System.Text.Json.Nodes;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Usuarios.Dtos;
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
        [typeof(SolicitudRegistro)] = """
            { "nombreUsuario": "ana.perez", "correo": "ana@ejemplo.com", "contrasena": "Tu-contrasena1" }
            """,
        [typeof(SolicitudCrearUsuario)] = """
            { "nombreUsuario": "ana.perez", "correo": "ana@ejemplo.com", "contrasena": "Tu-contrasena1", "rol": "Consulta" }
            """,
        [typeof(SolicitudActualizarUsuario)] = """
            { "nombreUsuario": "ana.perez", "correo": "ana@ejemplo.com", "rol": "Consulta", "activo": true, "contrasena": null }
            """,
        [typeof(UsuarioDto)] = """
            { "id": 3, "nombreUsuario": "ana.perez", "correo": "ana@ejemplo.com", "rol": "Consulta", "activo": true }
            """,
        [typeof(SolicitudCrearProducto)] = """
            {
              "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas", "categoria": "Periféricos",
              "lotes": [{ "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 59.90, "cantidad": 20 }]
            }
            """,
        [typeof(SolicitudActualizarProducto)] = """
            {
              "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas", "categoria": "Periféricos",
              "activo": true,
              "lotes": [
                { "id": 1, "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 62.00, "cantidad": 15 },
                { "numeroLote": "TEC-100-L2", "fechaIngreso": "2026-02-15", "precio": 65.50, "cantidad": 30 }
              ]
            }
            """,
        [typeof(ProductoDto)] = """
            {
              "id": 1, "codigo": "TEC-100", "nombre": "Teclado compacto", "descripcion": "Teclado de 60 teclas",
              "categoria": "Periféricos", "activo": true,
              "lotes": [{ "id": 1, "numeroLote": "TEC-100-L1", "fechaIngreso": "2026-01-15", "precio": 59.90, "cantidad": 20 }]
            }
            """,
        [typeof(ProductoResumenDto)] = """
            {
              "id": 1, "codigo": "TEC-100", "nombre": "Teclado compacto", "categoria": "Periféricos", "activo": true,
              "cantidadLotes": 2, "cantidadTotal": 45, "precioMinimo": 62.00, "precioMaximo": 65.50
            }
            """
    };

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema esquema && Ejemplos.TryGetValue(context.Type, out var ejemplo))
            esquema.Examples = [JsonNode.Parse(ejemplo)!];
    }
}
