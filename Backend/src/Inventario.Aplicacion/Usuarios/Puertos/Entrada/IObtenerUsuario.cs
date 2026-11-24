using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IObtenerUsuario
{
    Task<UsuarioDto> EjecutarAsync(int id, CancellationToken cancelacion);
}
