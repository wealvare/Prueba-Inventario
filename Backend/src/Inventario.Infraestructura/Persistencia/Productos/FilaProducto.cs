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
    public Producto AProducto(IEnumerable<FilaLote> lotes)
    {
        var producto = new Producto(Codigo, Nombre, Descripcion, Categoria);
        Rehidratacion.AsignarId(producto, Id);

        if (!Activo)
            producto.Desactivar();

        Rehidratacion.Lotes(producto).AddRange(lotes.Select(lote => lote.ALote(Id)));
        return producto;
    }
}
