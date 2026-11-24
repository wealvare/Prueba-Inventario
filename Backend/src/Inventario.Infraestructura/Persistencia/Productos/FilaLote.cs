using Inventario.Dominio.Productos;

namespace Inventario.Infraestructura.Persistencia.Productos;

internal sealed record FilaLote(int Id, string NumeroLote, DateTime FechaIngreso, decimal Precio, int Cantidad)
{
    public LoteProducto ALote(int productoId)
    {
        var lote = Rehidratacion.CrearLote(NumeroLote, DateOnly.FromDateTime(FechaIngreso), Precio, Cantidad);
        Rehidratacion.AsignarId(lote, Id);
        Rehidratacion.AsignarProductoId(lote, productoId);
        return lote;
    }
}
