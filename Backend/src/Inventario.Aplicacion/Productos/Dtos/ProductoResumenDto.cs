namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record ProductoResumenDto(
    int Id,
    string Codigo,
    string Nombre,
    string Categoria,
    bool Activo,
    int CantidadLotes,
    int CantidadTotal,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo);
