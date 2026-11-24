using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Dominio.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventario.Api.Productos;

[ApiController]
[Authorize]
[Route("api/productos")]
[Produces("application/json")]
[SwaggerTag("Productos y sus lotes")]
[SwaggerResponse(401, "Falta el token o no es válido.", typeof(ProblemDetails), "application/problem+json")]
public sealed class ProductosController(
    IListarProductos listarProductos,
    IObtenerProducto obtenerProducto,
    ICrearProducto crearProducto,
    IActualizarProducto actualizarProducto,
    IEliminarProducto eliminarProducto) : ControllerBase
{
    private const string SoloAdministrador = nameof(RolUsuario.Administrador);

    [HttpGet]
    [SwaggerOperation("Lista los productos", "Admite paginación, búsqueda y filtros por categoría, estado y rango de precio.")]
    [SwaggerResponse(200, "Página de productos.", typeof(ResultadoPaginado<ProductoResumenDto>))]
    [SwaggerResponse(400, "Los filtros no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    public async Task<ActionResult<ResultadoPaginado<ProductoResumenDto>>> Listar(
        [FromQuery] FiltroProductos filtro,
        CancellationToken cancelacion) =>
        Ok(await listarProductos.EjecutarAsync(filtro, cancelacion));

    [HttpGet("{id:int}")]
    [SwaggerOperation("Obtiene un producto", "Incluye todos sus lotes.")]
    [SwaggerResponse(200, "Producto encontrado.", typeof(ProductoDto))]
    [SwaggerResponse(404, "El producto no existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<ProductoDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await obtenerProducto.EjecutarAsync(id, cancelacion));

    [HttpPost]
    [Authorize(Roles = SoloAdministrador)]
    [SwaggerOperation("Crea un producto", "Solo para administradores.")]
    [SwaggerResponse(201, "Producto creado. El encabezado Location contiene su dirección.", typeof(ProductoDto))]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(403, "El rol no permite escribir.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "El código o el número de lote ya existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<ProductoDto>> Crear(SolicitudCrearProducto solicitud, CancellationToken cancelacion)
    {
        var producto = await crearProducto.EjecutarAsync(solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = producto.Id }, producto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = SoloAdministrador)]
    [SwaggerOperation(
        "Actualiza un producto",
        "Solo para administradores. Los lotes con id se modifican, los que no lo tienen se agregan y los ausentes se eliminan.")]
    [SwaggerResponse(200, "Producto actualizado.", typeof(ProductoDto))]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(403, "El rol no permite escribir.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(404, "El producto no existe.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "El código o el número de lote ya existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<ProductoDto>> Actualizar(
        int id,
        SolicitudActualizarProducto solicitud,
        CancellationToken cancelacion) =>
        Ok(await actualizarProducto.EjecutarAsync(id, solicitud, cancelacion));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = SoloAdministrador)]
    [SwaggerOperation("Elimina un producto", "Solo para administradores. El producto queda inactivo.")]
    [SwaggerResponse(204, "Producto eliminado.")]
    [SwaggerResponse(403, "El rol no permite escribir.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(404, "El producto no existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancelacion)
    {
        await eliminarProducto.EjecutarAsync(id, cancelacion);
        return NoContent();
    }
}
