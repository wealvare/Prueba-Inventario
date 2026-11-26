namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record SolicitudCrearProducto(
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Categoria,
    IReadOnlyList<SolicitudLote> Lotes,
    DateOnly? FechaExpira = null);
