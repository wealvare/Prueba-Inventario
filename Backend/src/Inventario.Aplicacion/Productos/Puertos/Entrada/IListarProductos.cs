using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface IListarProductos
{
    Task<ResultadoPaginado<ProductoResumenDto>> EjecutarAsync(FiltroProductos filtro, CancellationToken cancelacion);
}
