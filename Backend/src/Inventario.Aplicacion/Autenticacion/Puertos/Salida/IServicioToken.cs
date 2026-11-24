using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Autenticacion.Puertos.Salida;

public interface IServicioToken
{
    TokenAcceso Generar(Usuario usuario);
}
