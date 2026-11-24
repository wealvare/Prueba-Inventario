using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class ListarCategorias(IRepositorioProductos repositorio) : IListarCategorias
{
    public Task<IReadOnlyList<string>> EjecutarAsync(CancellationToken cancelacion) =>
        repositorio.ListarCategoriasAsync(cancelacion);
}
