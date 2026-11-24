using Inventario.Infraestructura.Configuracion;
using Serilog;

namespace Inventario.Api.Registros;

internal static class ConfiguracionRegistros
{
    public const string Origen = "Origen";
    public const string Correlacion = "Correlacion";

    private const string OrigenBackend = "backend";
    private const string SinCorrelacion = "-";

    public static LoggerConfiguration Configurar(
        this LoggerConfiguration registro,
        IConfiguration configuracion,
        IHostEnvironment entorno) => registro
            .ReadFrom.Configuration(configuracion)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Entorno", entorno.EnvironmentName)
            .Enrich.WithProperty(Origen, OrigenBackend)
            .Enrich.WithProperty(Correlacion, SinCorrelacion);

    public static void RegistrarFalloArranque(IConfiguration configuracion, IHostEnvironment entorno, Exception error)
    {
        using var registro = new LoggerConfiguration().Configurar(configuracion, entorno).CreateLogger();

        if (error is ExcepcionConfiguracion)
            registro.Fatal("No se pudo iniciar la Api. {Motivo}", error.Message);
        else
            registro.Fatal(error, "No se pudo iniciar la Api");
    }
}
