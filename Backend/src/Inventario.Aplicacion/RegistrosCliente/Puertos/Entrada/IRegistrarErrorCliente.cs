using Inventario.Aplicacion.RegistrosCliente.Dtos;

namespace Inventario.Aplicacion.RegistrosCliente.Puertos.Entrada;

public interface IRegistrarErrorCliente
{
    Task EjecutarAsync(SolicitudRegistroCliente solicitud, CancellationToken cancelacion);
}
