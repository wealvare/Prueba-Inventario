using Inventario.Dominio.Productos;

namespace Inventario.Infraestructura.Persistencia.Productos;

internal sealed record FilaLote(int Id, string NumeroLote, DateTime FechaIngreso, decimal Precio, int Cantidad)
{
    public LoteProducto ALote(int productoId) => LoteProducto.Reconstruir(
        Id, productoId, NumeroLote, DateOnly.FromDateTime(FechaIngreso), Precio, Cantidad);
}
