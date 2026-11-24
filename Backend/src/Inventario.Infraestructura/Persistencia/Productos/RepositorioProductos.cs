using System.Data.Common;
using System.Text.Json;
using Dapper;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;
using Inventario.Infraestructura.Persistencia.Conexion;
using Microsoft.Data.SqlClient;

namespace Inventario.Infraestructura.Persistencia.Productos;

public sealed class RepositorioProductos(IFabricaConexion fabrica, EjecutorTransaccion ejecutor) : IRepositorioProductos
{
    private const string IndiceCodigo = "UX_Productos_Codigo";
    private const string IndiceNumeroLote = "UX_LotesProducto_ProductoId_NumeroLote";
    private const int IndiceUnicoDuplicado = 2601;
    private const int RestriccionUnicaDuplicada = 2627;

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        await using var resultados = await conexion.QueryMultipleAsync(
            new CommandDefinition(SqlProductos.ObtenerConLotes, new { id }, cancellationToken: cancelacion));

        var fila = await resultados.ReadSingleOrDefaultAsync<FilaProducto>();
        var lotes = await resultados.ReadAsync<FilaLote>();

        return fila?.AProducto(lotes);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluido, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);

        return await conexion.ExecuteScalarAsync<bool>(
            new CommandDefinition(SqlProductos.ExisteCodigo, new { codigo, idExcluido }, cancellationToken: cancelacion));
    }

    public async Task<ResultadoPaginado<ProductoResumenDto>> ListarAsync(
        FiltroProductos filtro,
        CancellationToken cancelacion)
    {
        var (condiciones, parametros) = CondicionesProductos.Construir(filtro);

        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        await using var resultados = await conexion.QueryMultipleAsync(
            new CommandDefinition(SqlProductos.Listar(condiciones), parametros, cancellationToken: cancelacion));

        var total = await resultados.ReadSingleAsync<int>();
        var elementos = (await resultados.ReadAsync<ProductoResumenDto>()).AsList();

        return new ResultadoPaginado<ProductoResumenDto>(elementos, filtro.Pagina, filtro.TamanoPagina, total);
    }

    public async Task<IReadOnlyList<string>> ListarCategoriasAsync(CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);

        return (await conexion.QueryAsync<string>(
            new CommandDefinition(SqlProductos.ListarCategorias, cancellationToken: cancelacion))).AsList();
    }

    public async Task<Producto> AgregarAsync(Producto producto, CancellationToken cancelacion)
    {
        var id = 0;

        await GuardarAsync(
            producto,
            async transaccion => id = await InsertarAsync(producto, transaccion, cancelacion),
            cancelacion);

        return (await ObtenerPorIdAsync(id, cancelacion))!;
    }

    public async Task<Producto> ActualizarAsync(Producto producto, CancellationToken cancelacion)
    {
        await GuardarAsync(producto, async transaccion =>
        {
            await transaccion.Connection!.ExecuteAsync(new CommandDefinition(
                SqlProductos.Actualizar, Datos(producto), transaccion, cancellationToken: cancelacion));

            await SincronizarLotesAsync(producto, transaccion, cancelacion);
        }, cancelacion);

        return (await ObtenerPorIdAsync(producto.Id, cancelacion))!;
    }

    internal static async Task<int> InsertarAsync(
        Producto producto,
        DbTransaction transaccion,
        CancellationToken cancelacion)
    {
        var id = await transaccion.Connection!.ExecuteScalarAsync<int>(new CommandDefinition(
            SqlProductos.Insertar, Datos(producto), transaccion, cancellationToken: cancelacion));

        await InsertarLotesAsync(id, producto.Lotes, transaccion, cancelacion);
        return id;
    }

    private static async Task SincronizarLotesAsync(
        Producto producto,
        DbTransaction transaccion,
        CancellationToken cancelacion)
    {
        var conexion = transaccion.Connection!;

        if (producto.IdsLotesEliminados.Count > 0)
            await conexion.ExecuteAsync(new CommandDefinition(
                SqlProductos.EliminarLotes,
                new { productoId = producto.Id, ids = JsonSerializer.Serialize(producto.IdsLotesEliminados) },
                transaccion,
                cancellationToken: cancelacion));

        var existentes = producto.Lotes.Where(lote => lote.Id != 0).ToList();

        if (existentes.Count > 0)
        {
            var actualizados = await conexion.ExecuteAsync(new CommandDefinition(
                SqlProductos.ActualizarLotes,
                new { productoId = producto.Id, lotes = JsonSerializer.Serialize(existentes) },
                transaccion,
                cancellationToken: cancelacion));

            if (actualizados != existentes.Count)
                throw new ExcepcionLoteInexistente();
        }

        await InsertarLotesAsync(producto.Id, producto.Lotes.Where(lote => lote.Id == 0), transaccion, cancelacion);
    }

    private static async Task InsertarLotesAsync(
        int productoId,
        IEnumerable<LoteProducto> lotes,
        DbTransaction transaccion,
        CancellationToken cancelacion)
    {
        foreach (var lote in lotes)
            await transaccion.Connection!.ExecuteAsync(new CommandDefinition(
                SqlProductos.InsertarLote,
                new
                {
                    productoId,
                    lote.NumeroLote,
                    fechaIngreso = lote.FechaIngreso.ToDateTime(TimeOnly.MinValue),
                    lote.Precio,
                    lote.Cantidad
                },
                transaccion,
                cancellationToken: cancelacion));
    }

    private async Task GuardarAsync(
        Producto producto,
        Func<DbTransaction, Task> operacion,
        CancellationToken cancelacion)
    {
        try
        {
            await ejecutor.EjecutarAsync(operacion, cancelacion);
        }
        catch (SqlException error) when (EsDuplicado(error, IndiceCodigo))
        {
            throw new ExcepcionCodigoDuplicado(producto.Codigo);
        }
        catch (SqlException error) when (EsDuplicado(error, IndiceNumeroLote))
        {
            throw new ExcepcionLoteDuplicado(NumeroLoteDuplicado(error, producto));
        }
    }

    private static object Datos(Producto producto) => new
    {
        producto.Id,
        producto.Codigo,
        producto.Nombre,
        producto.Descripcion,
        producto.Categoria,
        producto.Activo
    };

    private static string NumeroLoteDuplicado(SqlException error, Producto producto) =>
        producto.Lotes
            .Select(lote => lote.NumeroLote)
            .FirstOrDefault(numero => error.Message.Contains($", {numero})", StringComparison.OrdinalIgnoreCase))
        ?? string.Empty;

    private static bool EsDuplicado(SqlException error, string indice) =>
        error.Number is IndiceUnicoDuplicado or RestriccionUnicaDuplicada
        && error.Message.Contains(indice, StringComparison.OrdinalIgnoreCase);
}
