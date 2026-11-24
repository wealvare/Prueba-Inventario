using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface ICrearProducto
{
    Task<ProductoDto> EjecutarAsync(SolicitudCrearProducto solicitud, CancellationToken cancelacion);
}
