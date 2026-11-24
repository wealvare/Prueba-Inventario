using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface IActualizarProducto
{
    Task<ProductoDto> EjecutarAsync(int id, SolicitudActualizarProducto solicitud, CancellationToken cancelacion);
}
