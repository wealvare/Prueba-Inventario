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

    private readonly List<Producto> _nuevos = [];
    private readonly List<(Producto Producto, int[] IdsLotes)> _editados = [];

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        await using var resultados = await conexion.QueryMultipleAsync(
            new CommandDefinition(SqlProductos.ObtenerConLotes, new { id }, cancellationToken: cancelacion));

        var fila = await resultados.ReadSingleOrDefaultAsync<FilaProducto>();
        var lotes = await resultados.ReadAsync<FilaLote>();

        return fila?.AProducto(lotes);
    }

    public async Task<Producto?> ObtenerParaEdicionAsync(int id, CancellationToken cancelacion)
    {
        var producto = await ObtenerPorIdAsync(id, cancelacion);

        if (producto is not null)
            _editados.Add((producto, producto.Lotes.Select(lote => lote.Id).ToArray()));

        return producto;
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

    public void Agregar(Producto producto) => _nuevos.Add(producto);

    public async Task GuardarCambiosAsync(CancellationToken cancelacion)
    {
        var asignaciones = new List<Action>();

        try
        {
            await ejecutor.EjecutarAsync(async transaccion =>
            {
                foreach (var producto in _nuevos)
                    await TraducirDuplicadosAsync(producto, () => InsertarAsync(producto, transaccion, asignaciones, cancelacion));

                foreach (var (producto, idsLotes) in _editados)
                    await TraducirDuplicadosAsync(
                        producto, () => ActualizarAsync(producto, idsLotes, transaccion, asignaciones, cancelacion));
            }, cancelacion);
        }
        finally
        {
            _nuevos.Clear();
            _editados.Clear();
        }

        asignaciones.ForEach(asignar => asignar());
    }

    private static async Task InsertarAsync(
        Producto producto,
        DbTransaction transaccion,
        List<Action> asignaciones,
        CancellationToken cancelacion)
    {
        var id = await transaccion.Connection!.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlProductos.Insertar, Datos(producto), transaccion, cancellationToken: cancelacion));
        asignaciones.Add(() => Rehidratacion.AsignarId(producto, id));

        await InsertarLotesAsync(id, producto.Lotes, transaccion, asignaciones, cancelacion);
    }

    private static async Task ActualizarAsync(
        Producto producto,
        int[] idsOriginales,
        DbTransaction transaccion,
        List<Action> asignaciones,
        CancellationToken cancelacion)
    {
        var conexion = transaccion.Connection!;
        await conexion.ExecuteAsync(
            new CommandDefinition(SqlProductos.Actualizar, Datos(producto), transaccion, cancellationToken: cancelacion));

        var existentes = producto.Lotes.Where(lote => lote.Id != 0).ToList();
        var eliminados = idsOriginales.Except(existentes.Select(lote => lote.Id)).ToList();

        if (eliminados.Count > 0)
            await conexion.ExecuteAsync(new CommandDefinition(
                SqlProductos.EliminarLotes,
                new { productoId = producto.Id, ids = JsonSerializer.Serialize(eliminados) },
                transaccion,
                cancellationToken: cancelacion));

        if (existentes.Count > 0)
        {
            var actualizados = await conexion.ExecuteAsync(new CommandDefinition(
                SqlProductos.ActualizarLotes,
                new { productoId = producto.Id, lotes = JsonSerializer.Serialize(existentes) },
                transaccion,
                cancellationToken: cancelacion));

            if (actualizados != existentes.Count)
                throw new ExcepcionReglaNegocio("Otro usuario modificó los lotes del producto. Vuelve a cargarlo e inténtalo de nuevo.");
        }

        await InsertarLotesAsync(
            producto.Id, producto.Lotes.Where(lote => lote.Id == 0), transaccion, asignaciones, cancelacion);
    }

    private static async Task InsertarLotesAsync(
        int productoId,
        IEnumerable<LoteProducto> lotes,
        DbTransaction transaccion,
        List<Action> asignaciones,
        CancellationToken cancelacion)
    {
        foreach (var lote in lotes)
        {
            var id = await transaccion.Connection!.ExecuteScalarAsync<int>(new CommandDefinition(
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

            asignaciones.Add(() =>
            {
                Rehidratacion.AsignarId(lote, id);
                Rehidratacion.AsignarProductoId(lote, productoId);
            });
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

    private static async Task TraducirDuplicadosAsync(Producto producto, Func<Task> operacion)
    {
        try
        {
            await operacion();
        }
        catch (SqlException error) when (EsDuplicado(error, IndiceCodigo))
        {
            throw new ExcepcionCodigoDuplicado(producto.Codigo);
        }
        catch (SqlException error) when (EsDuplicado(error, IndiceNumeroLote))
        {
            throw new ExcepcionReglaNegocio("Ya existe un lote con el mismo número en el producto.");
        }
    }

    private static bool EsDuplicado(SqlException error, string indice) =>
        error.Number is IndiceUnicoDuplicado or RestriccionUnicaDuplicada
        && error.Message.Contains(indice, StringComparison.OrdinalIgnoreCase);
}
