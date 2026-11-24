namespace Inventario.Aplicacion.Productos.Dtos;

public sealed record FiltroProductos(
    int Pagina = 1,
    int TamanoPagina = 10,
    string? Busqueda = null,
    string? Categoria = null,
    bool? Activo = null,
    decimal? PrecioMinimo = null,
    decimal? PrecioMaximo = null);
