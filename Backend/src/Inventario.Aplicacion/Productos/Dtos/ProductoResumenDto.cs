namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record ProductoResumenDto(
    int Id,
    string Codigo,
    string Nombre,
    string Categoria,
    bool Activo,
    DateTime? FechaExpira,
    int CantidadLotes,
    long CantidadTotal,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo);
