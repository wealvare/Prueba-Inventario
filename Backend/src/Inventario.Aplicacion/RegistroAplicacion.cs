using FluentValidation;
using Inventario.Aplicacion.Autenticacion.CasosUso;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Entrada;
using Inventario.Aplicacion.Autenticacion.Validadores;
using Inventario.Aplicacion.Productos.CasosUso;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Validadores;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Aplicacion;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<IIniciarSesion, IniciarSesion>();
        servicios.AddScoped<ICrearProducto, CrearProducto>();
        servicios.AddScoped<IActualizarProducto, ActualizarProducto>();
        servicios.AddScoped<IEliminarProducto, EliminarProducto>();
        servicios.AddScoped<IObtenerProducto, ObtenerProducto>();
        servicios.AddScoped<IListarProductos, ListarProductos>();

        servicios.AddSingleton<IValidator<SolicitudInicioSesion>, ValidadorInicioSesion>();
        servicios.AddSingleton<IValidator<SolicitudCrearProducto>, ValidadorCrearProducto>();
        servicios.AddSingleton<IValidator<SolicitudActualizarProducto>, ValidadorActualizarProducto>();
        servicios.AddSingleton<IValidator<FiltroProductos>, ValidadorFiltroProductos>();

        return servicios;
    }
}
