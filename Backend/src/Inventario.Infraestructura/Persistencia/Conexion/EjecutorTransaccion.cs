using System.Data.Common;

namespace Inventario.Infraestructura.Persistencia.Conexion;

public sealed class EjecutorTransaccion(IFabricaConexion fabrica)
{
    public async Task EjecutarAsync(Func<DbTransaction, Task> operacion, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        await using var transaccion = await conexion.BeginTransactionAsync(cancelacion);

        await operacion(transaccion);
        await transaccion.CommitAsync(cancelacion);
    }
}
