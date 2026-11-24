namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IDesactivarUsuario
{
    Task EjecutarAsync(int id, int idActor, CancellationToken cancelacion);
}
