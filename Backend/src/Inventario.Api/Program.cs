using DotNetEnv;
using Inventario.Api;
using Inventario.Aplicacion;
using Inventario.Infraestructura;
using Inventario.Infraestructura.Persistencia;
using Serilog;

Env.NoClobber().TraversePath().Load();

var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddSerilog((servicios, registro) => registro
    .ReadFrom.Configuration(constructor.Configuration)
    .ReadFrom.Services(servicios)
    .Enrich.FromLogContext());

constructor.Services
    .AgregarAplicacion()
    .AgregarInfraestructura(constructor.Configuration)
    .AgregarApi(constructor.Configuration);

if (constructor.Environment.IsDevelopment())
    constructor.Services.AgregarInicializacionBaseDatos(constructor.Configuration);

var aplicacion = constructor.Build();

if (aplicacion.Environment.IsDevelopment())
{
    await using var ambito = aplicacion.Services.CreateAsyncScope();
    await ambito.ServiceProvider
        .GetRequiredService<InicializadorBaseDatos>()
        .EjecutarAsync(aplicacion.Lifetime.ApplicationStopping);
}

aplicacion.UsarApi();

aplicacion.Run();
