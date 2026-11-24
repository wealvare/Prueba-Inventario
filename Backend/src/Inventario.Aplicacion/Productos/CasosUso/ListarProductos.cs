using FluentValidation;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class ListarProductos(
    IRepositorioProductos repositorio,
    IValidator<FiltroProductos> validador) : IListarProductos
{
    public async Task<ResultadoPaginado<ProductoResumenDto>> EjecutarAsync(
        FiltroProductos filtro,
        CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(filtro, cancelacion);

        return await repositorio.ListarAsync(filtro, cancelacion);
    }
}
