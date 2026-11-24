using Inventario.Dominio.Productos;

namespace Inventario.Infraestructura.Persistencia.Productos;

internal sealed record FilaProducto(
    int Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    string Categoria,
    bool Activo)
{
    public Producto AProducto(IEnumerable<FilaLote> lotes) => Producto.Reconstruir(
        Id, Codigo, Nombre, Descripcion, Categoria, Activo, lotes.Select(lote => lote.ALote(Id)));
}
