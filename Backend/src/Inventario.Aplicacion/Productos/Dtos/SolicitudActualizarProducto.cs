namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record SolicitudActualizarProducto(
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Categoria,
    bool? Activo,
    IReadOnlyList<SolicitudLote> Lotes);
