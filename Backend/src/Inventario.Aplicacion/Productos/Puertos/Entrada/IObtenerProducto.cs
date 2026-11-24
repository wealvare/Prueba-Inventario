using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface IObtenerProducto
{
    Task<ProductoDto> EjecutarAsync(int id, CancellationToken cancelacion);
}
