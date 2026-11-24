using Inventario.Aplicacion.Autenticacion.Dtos;

namespace Inventario.Aplicacion.Autenticacion.Puertos.Entrada;

public interface IIniciarSesion
{
    Task<RespuestaInicioSesion> EjecutarAsync(SolicitudInicioSesion solicitud, CancellationToken cancelacion);
}
