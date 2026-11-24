using DotNetEnv;
using Inventario.Infraestructura;
using Inventario.Infraestructura.Persistencia;

Env.NoClobber().TraversePath().Load();

var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AgregarInfraestructura(constructor.Configuration);

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

aplicacion.Run();
