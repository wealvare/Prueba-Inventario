using Dapper;
using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Infraestructura.Persistencia.Productos;

internal static class CondicionesProductos
{
    public static (string Sql, DynamicParameters Parametros) Construir(FiltroProductos filtro)
    {
        var condiciones = new List<string> { "producto.Activo = @activo" };
        var parametros = new DynamicParameters();
        parametros.Add("activo", filtro.Activo ?? true);
        parametros.Add("desplazamiento", (filtro.Pagina - 1) * filtro.TamanoPagina);
        parametros.Add("tamanoPagina", filtro.TamanoPagina);

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            condiciones.Add("""
                (CHARINDEX(@busqueda, producto.Codigo) > 0
                    OR CHARINDEX(@busqueda, producto.Nombre) > 0
                    OR CHARINDEX(@busqueda, producto.Descripcion) > 0)
                """);
            parametros.Add("busqueda", filtro.Busqueda.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            condiciones.Add("producto.Categoria = @categoria");
            parametros.Add("categoria", filtro.Categoria.Trim());
        }

        if (filtro.PrecioMinimo is not null || filtro.PrecioMaximo is not null)
        {
            condiciones.Add("""
                EXISTS (
                    SELECT 1
                    FROM dbo.LotesProducto AS lote
                    WHERE lote.ProductoId = producto.Id
                        AND (@precioMinimo IS NULL OR lote.Precio >= @precioMinimo)
                        AND (@precioMaximo IS NULL OR lote.Precio <= @precioMaximo)
                )
                """);
            parametros.Add("precioMinimo", filtro.PrecioMinimo);
            parametros.Add("precioMaximo", filtro.PrecioMaximo);
        }

        return (string.Join(" AND ", condiciones), parametros);
    }
}
