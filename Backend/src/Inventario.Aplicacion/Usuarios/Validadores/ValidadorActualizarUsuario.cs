using FluentValidation;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Validadores;

public sealed class ValidadorActualizarUsuario : AbstractValidator<SolicitudActualizarUsuario>
{
    public ValidadorActualizarUsuario()
    {
        RuleFor(solicitud => solicitud.NombreUsuario).NombreUsuarioValido();
        RuleFor(solicitud => solicitud.Correo).CorreoValido();
        RuleFor(solicitud => solicitud.Rol).RolValido();
        RuleFor(solicitud => solicitud.Activo).NotNull().WithMessage("Indica si el usuario está activo.");
        RuleFor(solicitud => solicitud.Contrasena)
            .Must(PoliticaContrasena.Cumple)
            .When(solicitud => solicitud.Contrasena is not null)
            .WithMessage(PoliticaContrasena.Mensaje);
    }
}
