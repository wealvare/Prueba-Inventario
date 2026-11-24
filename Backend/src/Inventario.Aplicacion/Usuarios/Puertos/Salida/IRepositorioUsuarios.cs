using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Puertos.Salida;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int? idExcluido, CancellationToken cancelacion);

    Task<bool> ExisteCorreoAsync(string correo, int? idExcluido, CancellationToken cancelacion);

    Task<bool> ExisteOtroAdministradorActivoAsync(int idExcluido, CancellationToken cancelacion);

    Task<ResultadoPaginado<UsuarioDto>> ListarAsync(FiltroUsuarios filtro, CancellationToken cancelacion);

    Task<Usuario> AgregarAsync(Usuario usuario, CancellationToken cancelacion);

    Task<Usuario> ActualizarAsync(Usuario usuario, CancellationToken cancelacion);
}
