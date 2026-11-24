using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface ICrearUsuario
{
    Task<UsuarioDto> EjecutarAsync(SolicitudCrearUsuario solicitud, CancellationToken cancelacion);
}
