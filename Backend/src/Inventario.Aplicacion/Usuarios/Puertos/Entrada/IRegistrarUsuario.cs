using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IRegistrarUsuario
{
    Task<UsuarioDto> EjecutarAsync(SolicitudRegistro solicitud, CancellationToken cancelacion);
}
