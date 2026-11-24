using FluentValidation;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Mapeos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class CrearProducto(
    IRepositorioProductos repositorio,
    IValidator<SolicitudCrearProducto> validador) : ICrearProducto
{
    public async Task<ProductoDto> EjecutarAsync(SolicitudCrearProducto solicitud, CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);

        var producto = new Producto(solicitud.Codigo, solicitud.Nombre, solicitud.Descripcion, solicitud.Categoria);
        producto.SincronizarLotes(solicitud.Lotes.ADatos());

        if (await repositorio.ExisteCodigoAsync(producto.Codigo, null, cancelacion))
            throw new ExcepcionCodigoDuplicado(producto.Codigo);

        repositorio.Agregar(producto);
        await repositorio.GuardarCambiosAsync(cancelacion);

        return producto.ADto();
    }
}
