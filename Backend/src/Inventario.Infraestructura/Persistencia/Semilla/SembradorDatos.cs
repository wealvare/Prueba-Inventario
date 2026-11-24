using Dapper;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia.Conexion;

namespace Inventario.Infraestructura.Persistencia.Semilla;

public sealed class SembradorDatos(
    IFabricaConexion fabrica,
    IRepositorioUsuarios usuarios,
    IRepositorioProductos productos,
    IHasheadorContrasena hasheador,
    OpcionesSemilla opciones)
{
    private const string InsertarUsuario = """
        INSERT INTO dbo.Usuarios (NombreUsuario, Correo, HashContrasena, Rol)
        VALUES (@NombreUsuario, @Correo, @HashContrasena, @rol);
        """;

    private const string HayProductos = """
        SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Productos) THEN 1 ELSE 0 END AS BIT);
        """;

    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        await SembrarUsuariosAsync(cancelacion);
        await SembrarProductosAsync(cancelacion);
    }

    private async Task SembrarUsuariosAsync(CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);

        foreach (var semilla in opciones.Usuarios)
        {
            if (await usuarios.ObtenerPorNombreUsuarioAsync(semilla.NombreUsuario, cancelacion) is not null)
                continue;

            var usuario = new Usuario(
                semilla.NombreUsuario, semilla.Correo, hasheador.Hashear(semilla.Contrasena), semilla.Rol);

            await conexion.ExecuteAsync(new CommandDefinition(
                InsertarUsuario,
                new { usuario.NombreUsuario, usuario.Correo, usuario.HashContrasena, rol = usuario.Rol.ToString() },
                cancellationToken: cancelacion));
        }
    }

    private async Task SembrarProductosAsync(CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);

        if (await conexion.ExecuteScalarAsync<bool>(new CommandDefinition(HayProductos, cancellationToken: cancelacion)))
            return;

        foreach (var producto in CatalogoSemilla.Crear())
            productos.Agregar(producto);

        await productos.GuardarCambiosAsync(cancelacion);
    }
}
