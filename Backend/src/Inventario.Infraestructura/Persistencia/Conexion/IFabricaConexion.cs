using System.Data.Common;

namespace Inventario.Infraestructura.Persistencia.Conexion;

public interface IFabricaConexion
{
    Task<DbConnection> AbrirAsync(CancellationToken cancelacion);
}
