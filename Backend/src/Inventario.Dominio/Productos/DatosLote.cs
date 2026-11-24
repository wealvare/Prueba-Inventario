namespace Inventario.Dominio.Productos;

public sealed record DatosLote(int? Id, string NumeroLote, DateOnly FechaIngreso, decimal Precio, int Cantidad);
