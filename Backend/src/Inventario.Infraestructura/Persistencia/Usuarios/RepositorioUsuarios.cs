using Dapper;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Persistencia.Conexion;

namespace Inventario.Infraestructura.Persistencia.Usuarios;

public sealed class RepositorioUsuarios(IFabricaConexion fabrica) : IRepositorioUsuarios
{
    private const string ObtenerPorNombre = """
        SELECT Id, NombreUsuario, Correo, HashContrasena, Rol, Activo
        FROM dbo.Usuarios
        WHERE NombreUsuario = @nombreUsuario;
        """;

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        var fila = await conexion.QuerySingleOrDefaultAsync<FilaUsuario>(
            new CommandDefinition(ObtenerPorNombre, new { nombreUsuario }, cancellationToken: cancelacion));

        return fila?.AUsuario();
    }
}
