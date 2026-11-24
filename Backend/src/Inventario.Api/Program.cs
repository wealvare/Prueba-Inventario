using DotNetEnv;
using Inventario.Api;
using Inventario.Api.Registros;
using Inventario.Aplicacion;
using Inventario.Infraestructura;
using Inventario.Infraestructura.Persistencia;
using Serilog;

Env.NoClobber().TraversePath().Load();

var constructor = WebApplication.CreateBuilder(args);

try
{
    constructor.Services.AddSerilog((servicios, registro) => registro
        .Configurar(constructor.Configuration, constructor.Environment)
        .ReadFrom.Services(servicios), preserveStaticLogger: true);

    constructor.Services
        .AgregarAplicacion()
        .AgregarInfraestructura(constructor.Configuration)
        .AgregarApi(constructor.Configuration);

    if (constructor.Environment.IsDevelopment())
        constructor.Services.AgregarInicializacionBaseDatos(constructor.Configuration);

    await using var aplicacion = constructor.Build();

    if (aplicacion.Environment.IsDevelopment())
    {
        await using var ambito = aplicacion.Services.CreateAsyncScope();
        await ambito.ServiceProvider
            .GetRequiredService<InicializadorBaseDatos>()
            .EjecutarAsync(aplicacion.Lifetime.ApplicationStopping);
    }

    aplicacion.UsarApi();

    await aplicacion.RunAsync();
    return 0;
}
catch (Exception error) when (error is not HostAbortedException)
{
    ConfiguracionRegistros.RegistrarFalloArranque(constructor.Configuration, constructor.Environment, error);
    return 1;
}
