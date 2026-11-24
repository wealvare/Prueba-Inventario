using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Excepciones;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class EliminarProducto(IRepositorioProductos repositorio) : IEliminarProducto
{
    public async Task EjecutarAsync(int id, CancellationToken cancelacion)
    {
        var producto = await repositorio.ObtenerParaEdicionAsync(id, cancelacion)
            ?? throw new ExcepcionProductoNoEncontrado(id);

        producto.Desactivar();
        await repositorio.GuardarCambiosAsync(cancelacion);
    }
}
