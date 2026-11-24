using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.Puertos.Salida;

public interface IRepositorioProductos
{
    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);
    Task<bool> ExisteCodigoAsync(string codigo, int? idExcluido, CancellationToken cancelacion);
    Task<ResultadoPaginado<ProductoResumenDto>> ListarAsync(FiltroProductos filtro, CancellationToken cancelacion);
    Task<Producto> AgregarAsync(Producto producto, CancellationToken cancelacion);
    Task<Producto> ActualizarAsync(Producto producto, CancellationToken cancelacion);
}
