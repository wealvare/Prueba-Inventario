using System.Data.Common;
using Dapper;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Persistencia.Conexion;
using Microsoft.Data.SqlClient;

namespace Inventario.Infraestructura.Persistencia.Usuarios;

public sealed class RepositorioUsuarios(IFabricaConexion fabrica, EjecutorTransaccion ejecutor) : IRepositorioUsuarios
{
    private const string IndiceNombreUsuario = "UX_Usuarios_NombreUsuario";
    private const string IndiceCorreo = "UX_Usuarios_Correo";

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion) =>
        ObtenerAsync(SqlUsuarios.ObtenerPorNombre, new { nombreUsuario }, cancelacion);

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancelacion) =>
        ObtenerAsync(SqlUsuarios.ObtenerPorId, new { id }, cancelacion);

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int? idExcluido, CancellationToken cancelacion) =>
        ExisteAsync(SqlUsuarios.ExisteNombre, new { nombreUsuario, idExcluido }, cancelacion);

    public Task<bool> ExisteCorreoAsync(string correo, int? idExcluido, CancellationToken cancelacion) =>
        ExisteAsync(SqlUsuarios.ExisteCorreo, new { correo, idExcluido }, cancelacion);

    public Task<bool> ExisteOtroAdministradorActivoAsync(int idExcluido, CancellationToken cancelacion) =>
        ExisteAsync(SqlUsuarios.ExisteOtroAdministrador, new { id = idExcluido }, cancelacion);

    public async Task<ResultadoPaginado<UsuarioDto>> ListarAsync(FiltroUsuarios filtro, CancellationToken cancelacion)
    {
        var parametros = new
        {
            desplazamiento = (filtro.Pagina - 1) * filtro.TamanoPagina,
            tamanoPagina = filtro.TamanoPagina
        };

        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        await using var resultados = await conexion.QueryMultipleAsync(
            new CommandDefinition(SqlUsuarios.Listar, parametros, cancellationToken: cancelacion));

        var total = await resultados.ReadSingleAsync<int>();
        var elementos = (await resultados.ReadAsync<UsuarioDto>()).AsList();

        return new ResultadoPaginado<UsuarioDto>(elementos, filtro.Pagina, filtro.TamanoPagina, total);
    }

    public async Task<Usuario> AgregarAsync(Usuario usuario, CancellationToken cancelacion)
    {
        var id = 0;

        await GuardarAsync(
            usuario,
            async transaccion => id = await transaccion.Connection!.ExecuteScalarAsync<int>(new CommandDefinition(
                SqlUsuarios.Insertar, Datos(usuario), transaccion, cancellationToken: cancelacion)),
            cancelacion);

        return (await ObtenerPorIdAsync(id, cancelacion))!;
    }

    public async Task<Usuario> ActualizarAsync(Usuario usuario, CancellationToken cancelacion)
    {
        await GuardarAsync(usuario, async transaccion =>
        {
            var conexion = transaccion.Connection!;

            if (!usuario.EsAdministradorActivo
                && !await conexion.ExecuteScalarAsync<bool>(new CommandDefinition(
                    SqlUsuarios.ExisteOtroAdministradorConBloqueo,
                    new { id = usuario.Id },
                    transaccion,
                    cancellationToken: cancelacion)))
                throw new ExcepcionUltimoAdministrador();

            await conexion.ExecuteAsync(new CommandDefinition(
                SqlUsuarios.Actualizar, Datos(usuario), transaccion, cancellationToken: cancelacion));
        }, cancelacion);

        return (await ObtenerPorIdAsync(usuario.Id, cancelacion))!;
    }

    private async Task<Usuario?> ObtenerAsync(string sql, object parametros, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);
        var fila = await conexion.QuerySingleOrDefaultAsync<FilaUsuario>(
            new CommandDefinition(sql, parametros, cancellationToken: cancelacion));

        return fila?.AUsuario();
    }

    private async Task<bool> ExisteAsync(string sql, object parametros, CancellationToken cancelacion)
    {
        await using var conexion = await fabrica.AbrirAsync(cancelacion);

        return await conexion.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, parametros, cancellationToken: cancelacion));
    }

    private async Task GuardarAsync(
        Usuario usuario,
        Func<DbTransaction, Task> operacion,
        CancellationToken cancelacion)
    {
        try
        {
            await ejecutor.EjecutarAsync(operacion, cancelacion);
        }
        catch (SqlException error) when (error.EsDuplicado(IndiceNombreUsuario))
        {
            throw new ExcepcionNombreUsuarioDuplicado(usuario.NombreUsuario);
        }
        catch (SqlException error) when (error.EsDuplicado(IndiceCorreo))
        {
            throw new ExcepcionCorreoDuplicado(usuario.Correo);
        }
    }

    private static object Datos(Usuario usuario) => new
    {
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Correo,
        usuario.HashContrasena,
        rol = usuario.Rol.ToString(),
        usuario.Activo
    };
}
