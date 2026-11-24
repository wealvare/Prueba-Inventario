using System.Data.Common;
using Inventario.Infraestructura.Configuracion;
using Microsoft.Data.SqlClient;

namespace Inventario.Infraestructura.Persistencia.Conexion;

public sealed class FabricaConexion(OpcionesBaseDatos opciones) : IFabricaConexion
{
    public async Task<DbConnection> AbrirAsync(CancellationToken cancelacion)
    {
        var conexion = new SqlConnection(opciones.CadenaConexion);

        try
        {
            await conexion.OpenAsync(cancelacion);
            return conexion;
        }
        catch
        {
            await conexion.DisposeAsync();
            throw;
        }
    }
}
