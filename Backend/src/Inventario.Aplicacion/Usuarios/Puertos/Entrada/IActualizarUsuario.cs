using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IActualizarUsuario
{
    Task<UsuarioDto> EjecutarAsync(
        int id,
        SolicitudActualizarUsuario solicitud,
        int idActor,
        CancellationToken cancelacion);
}
