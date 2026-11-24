namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface IEliminarProducto
{
    Task EjecutarAsync(int id, CancellationToken cancelacion);
}
