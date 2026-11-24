using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Mapeos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Excepciones;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class ObtenerProducto(IRepositorioProductos repositorio) : IObtenerProducto
{
    public async Task<ProductoDto> EjecutarAsync(int id, CancellationToken cancelacion)
    {
        var producto = await repositorio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new ExcepcionProductoNoEncontrado(id);

        return producto.ADto();
    }
}
