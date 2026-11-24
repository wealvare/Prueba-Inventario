using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Autenticacion.Puertos.Salida;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
}
