using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IListarUsuarios
{
    Task<ResultadoPaginado<UsuarioDto>> EjecutarAsync(FiltroUsuarios filtro, CancellationToken cancelacion);
}
