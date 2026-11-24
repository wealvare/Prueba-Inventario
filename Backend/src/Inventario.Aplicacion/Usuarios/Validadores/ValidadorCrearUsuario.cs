using FluentValidation;
using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Validadores;

public sealed class ValidadorCrearUsuario : AbstractValidator<SolicitudCrearUsuario>
{
    public ValidadorCrearUsuario()
    {
        RuleFor(solicitud => solicitud.NombreUsuario).NombreUsuarioValido();
        RuleFor(solicitud => solicitud.Correo).CorreoValido();
        RuleFor(solicitud => solicitud.Contrasena).ContrasenaValida();
        RuleFor(solicitud => solicitud.Rol).RolValido();
    }
}
