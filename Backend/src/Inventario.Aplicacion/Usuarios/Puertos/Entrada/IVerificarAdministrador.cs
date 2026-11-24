namespace Inventario.Aplicacion.Usuarios.Puertos.Entrada;

public interface IVerificarAdministrador
{
    Task<bool> EjecutarAsync(int id, CancellationToken cancelacion);
}
