namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record ProductoDto(
    int Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    string Categoria,
    bool Activo,
    IReadOnlyList<LoteDto> Lotes);
