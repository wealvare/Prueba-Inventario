using FluentValidation;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Mapeos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.CasosUso;

public sealed class ActualizarProducto(
    IRepositorioProductos repositorio,
    IValidator<SolicitudActualizarProducto> validador) : IActualizarProducto
{
    public async Task<ProductoDto> EjecutarAsync(
        int id,
        SolicitudActualizarProducto solicitud,
        CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);

        var producto = await repositorio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new ExcepcionProductoNoEncontrado(id);

        var codigo = Producto.NormalizarCodigo(solicitud.Codigo);

        if (await repositorio.ExisteCodigoAsync(codigo, id, cancelacion))
            throw new ExcepcionCodigoDuplicado(codigo);

        producto.Actualizar(
            codigo,
            solicitud.Nombre,
            solicitud.Descripcion,
            solicitud.Categoria,
            solicitud.Activo!.Value,
            solicitud.FechaExpira);
        producto.SincronizarLotes(solicitud.Lotes.ADatos());

        var guardado = await repositorio.ActualizarAsync(producto, cancelacion);

        return guardado.ADto();
    }
}
