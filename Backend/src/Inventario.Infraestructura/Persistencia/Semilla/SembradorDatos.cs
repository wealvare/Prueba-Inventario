using System.Data.Common;
using Dapper;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.Productos;

namespace Inventario.Infraestructura.Persistencia.Semilla;

public sealed class SembradorDatos(
    EjecutorTransaccion ejecutor,
    IHasheadorContrasena hasheador,
    OpcionesSemilla opciones)
{
    private const string ExisteUsuario = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Usuarios WITH (TABLOCKX, HOLDLOCK) WHERE NombreUsuario = @NombreUsuario
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    private const string InsertarUsuario = """
        INSERT INTO dbo.Usuarios (NombreUsuario, Correo, HashContrasena, Rol)
        VALUES (@NombreUsuario, @Correo, @HashContrasena, @rol);
        """;

    private const string HayProductos = """
        SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Productos WITH (TABLOCKX, HOLDLOCK)) THEN 1 ELSE 0 END AS BIT);
        """;

    public Task EjecutarAsync(CancellationToken cancelacion) => ejecutor.EjecutarAsync(async transaccion =>
    {
        await SembrarUsuariosAsync(transaccion, cancelacion);
        await SembrarProductosAsync(transaccion, cancelacion);
    }, cancelacion);

    private async Task SembrarUsuariosAsync(DbTransaction transaccion, CancellationToken cancelacion)
    {
        var conexion = transaccion.Connection!;

        foreach (var semilla in opciones.Usuarios)
        {
            if (await conexion.ExecuteScalarAsync<bool>(new CommandDefinition(
                ExisteUsuario, new { semilla.NombreUsuario }, transaccion, cancellationToken: cancelacion)))
                continue;

            var usuario = new Usuario(
                semilla.NombreUsuario, semilla.Correo, hasheador.Hashear(semilla.Contrasena), semilla.Rol);

            await conexion.ExecuteAsync(new CommandDefinition(
                InsertarUsuario,
                new { usuario.NombreUsuario, usuario.Correo, usuario.HashContrasena, rol = usuario.Rol.ToString() },
                transaccion,
                cancellationToken: cancelacion));
        }
    }

    private static async Task SembrarProductosAsync(DbTransaction transaccion, CancellationToken cancelacion)
    {
        if (await transaccion.Connection!.ExecuteScalarAsync<bool>(
            new CommandDefinition(HayProductos, transaction: transaccion, cancellationToken: cancelacion)))
            return;

        foreach (var producto in CatalogoSemilla.Crear())
            await RepositorioProductos.InsertarAsync(producto, transaccion, cancelacion);
    }
}
