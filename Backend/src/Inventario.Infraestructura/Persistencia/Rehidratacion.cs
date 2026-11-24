using System.Runtime.CompilerServices;
using Inventario.Dominio.Productos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Infraestructura.Persistencia;

internal static class Rehidratacion
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    public static extern void AsignarId(Producto producto, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    public static extern void AsignarId(LoteProducto lote, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    public static extern void AsignarId(Usuario usuario, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ProductoId")]
    public static extern void AsignarProductoId(LoteProducto lote, int productoId);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    public static extern LoteProducto CrearLote(string numeroLote, DateOnly fechaIngreso, decimal precio, int cantidad);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_lotes")]
    public static extern ref List<LoteProducto> Lotes(Producto producto);
}
