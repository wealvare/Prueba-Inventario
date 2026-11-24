namespace Inventario.Aplicacion.Comun;

public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Elementos, int Pagina, int TamanoPagina, int TotalElementos)
{
    public int TotalPaginas => (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);
}
