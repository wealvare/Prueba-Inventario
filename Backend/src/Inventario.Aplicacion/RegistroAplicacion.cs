using FluentValidation;
using Inventario.Aplicacion.Autenticacion.CasosUso;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Entrada;
using Inventario.Aplicacion.Autenticacion.Validadores;
using Inventario.Aplicacion.Productos.CasosUso;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Aplicacion.Productos.Validadores;
using Inventario.Aplicacion.RegistrosCliente.CasosUso;
using Inventario.Aplicacion.RegistrosCliente.Dtos;
using Inventario.Aplicacion.RegistrosCliente.Puertos.Entrada;
using Inventario.Aplicacion.RegistrosCliente.Validadores;
using Inventario.Aplicacion.Usuarios.CasosUso;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Validadores;
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
        servicios.AddScoped<IListarCategorias, ListarCategorias>();
        servicios.AddScoped<IRegistrarUsuario, RegistrarUsuario>();
        servicios.AddScoped<ICrearUsuario, CrearUsuario>();
        servicios.AddScoped<IActualizarUsuario, ActualizarUsuario>();
        servicios.AddScoped<IDesactivarUsuario, DesactivarUsuario>();
        servicios.AddScoped<IObtenerUsuario, ObtenerUsuario>();
        servicios.AddScoped<IListarUsuarios, ListarUsuarios>();
        servicios.AddScoped<IVerificarAdministrador, VerificarAdministrador>();
        servicios.AddScoped<IRegistrarErrorCliente, RegistrarErrorCliente>();

        servicios.AddSingleton<IValidator<SolicitudInicioSesion>, ValidadorInicioSesion>();
        servicios.AddSingleton<IValidator<SolicitudCrearProducto>, ValidadorCrearProducto>();
        servicios.AddSingleton<IValidator<SolicitudActualizarProducto>, ValidadorActualizarProducto>();
        servicios.AddSingleton<IValidator<FiltroProductos>, ValidadorFiltroProductos>();
        servicios.AddSingleton<IValidator<SolicitudRegistro>, ValidadorRegistro>();
        servicios.AddSingleton<IValidator<SolicitudCrearUsuario>, ValidadorCrearUsuario>();
        servicios.AddSingleton<IValidator<SolicitudActualizarUsuario>, ValidadorActualizarUsuario>();
        servicios.AddSingleton<IValidator<FiltroUsuarios>, ValidadorFiltroUsuarios>();
        servicios.AddSingleton<IValidator<SolicitudRegistroCliente>, ValidadorRegistroCliente>();

        return servicios;
    }
}
