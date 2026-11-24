namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record LoteDto(int Id, string NumeroLote, DateOnly FechaIngreso, decimal Precio, int Cantidad);
