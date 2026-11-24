using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.Productos;
using Inventario.Infraestructura.Persistencia.Semilla;
using Inventario.Infraestructura.Persistencia.Usuarios;
using Inventario.Infraestructura.Seguridad;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Infraestructura;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.AddSingleton(OpcionesBaseDatos.Leer(configuracion));
        servicios.AddSingleton(OpcionesJwt.Leer(configuracion));
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<IFabricaConexion, FabricaConexion>();
        servicios.AddSingleton<EjecutorTransaccion>();
        servicios.AddSingleton<IHasheadorContrasena, HasheadorContrasena>();
        servicios.AddSingleton<IServicioToken, ServicioToken>();
        servicios.AddScoped<IRepositorioProductos, RepositorioProductos>();
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();

        return servicios;
    }

    public static IServiceCollection AgregarInicializacionBaseDatos(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.AddSingleton(OpcionesSemilla.Leer(configuracion));
        servicios.AddSingleton<MigradorEsquema>();
        servicios.AddScoped<SembradorDatos>();
        servicios.AddScoped<InicializadorBaseDatos>();

        return servicios;
    }
}
