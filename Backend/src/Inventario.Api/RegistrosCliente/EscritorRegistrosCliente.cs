using Inventario.Api.Registros;
using Inventario.Aplicacion.RegistrosCliente.Dtos;
using Inventario.Aplicacion.RegistrosCliente.Puertos.Salida;
using Serilog.Context;

namespace Inventario.Api.RegistrosCliente;

internal sealed class EscritorRegistrosCliente(ILogger<EscritorRegistrosCliente> registrador) : IEscritorRegistrosCliente
{
    private const string OrigenFrontend = "frontend";

    public void Escribir(RegistroCliente registro)
    {
        var nivel = registro.Nivel == NivelRegistroCliente.Error ? LogLevel.Error : LogLevel.Warning;

        using (LogContext.PushProperty(ConfiguracionRegistros.Origen, OrigenFrontend))
        using (registro.Correlacion is null
            ? null
            : LogContext.PushProperty(ConfiguracionRegistros.Correlacion, registro.Correlacion))
        {
            registrador.Log(
                nivel,
                "{MensajeCliente} en {RutaCliente}, tipo {TipoCliente}",
                registro.Mensaje,
                registro.Ruta,
                registro.Tipo ?? "no indicado");
        }
    }
}
