using Inventario.Aplicacion.RegistrosCliente.Dtos;

namespace Inventario.Aplicacion.RegistrosCliente.Puertos.Salida;

public interface IEscritorRegistrosCliente
{
    void Escribir(RegistroCliente registro);
}
