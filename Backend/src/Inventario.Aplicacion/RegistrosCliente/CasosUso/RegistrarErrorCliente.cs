using FluentValidation;
using Inventario.Aplicacion.RegistrosCliente.Dtos;
using Inventario.Aplicacion.RegistrosCliente.Puertos.Entrada;
using Inventario.Aplicacion.RegistrosCliente.Puertos.Salida;

namespace Inventario.Aplicacion.RegistrosCliente.CasosUso;

public sealed class RegistrarErrorCliente(
    IEscritorRegistrosCliente escritor,
    IValidator<SolicitudRegistroCliente> validador) : IRegistrarErrorCliente
{
    public async Task EjecutarAsync(SolicitudRegistroCliente solicitud, CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);

        escritor.Escribir(new RegistroCliente(
            solicitud.Nivel!.Value,
            DatosSensibles.Ocultar(solicitud.Mensaje),
            DatosSensibles.Ocultar(solicitud.Ruta),
            solicitud.Tipo,
            solicitud.Correlacion));
    }
}
