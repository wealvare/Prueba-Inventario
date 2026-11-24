namespace Inventario.Aplicacion.Productos.Puertos.Entrada;

public interface IListarCategorias
{
    Task<IReadOnlyList<string>> EjecutarAsync(CancellationToken cancelacion);
}
