using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.Mapeos;

internal static class MapeoProducto
{
    public static ProductoDto ADto(this Producto producto) => new(
        producto.Id,
        producto.Codigo,
        producto.Nombre,
        producto.Descripcion,
        producto.Categoria,
        producto.Activo,
        producto.Lotes
            .Select(lote => new LoteDto(lote.Id, lote.NumeroLote, lote.FechaIngreso, lote.Precio, lote.Cantidad))
            .ToList());

    public static IReadOnlyCollection<DatosLote> ADatos(this IEnumerable<SolicitudLote> lotes) => lotes
        .Select(lote => new DatosLote(lote.Id, lote.NumeroLote, lote.FechaIngreso, lote.Precio, lote.Cantidad))
        .ToList();
}
