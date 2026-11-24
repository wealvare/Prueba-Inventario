namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record SolicitudLote(int? Id, string NumeroLote, DateOnly FechaIngreso, decimal Precio, int Cantidad);
