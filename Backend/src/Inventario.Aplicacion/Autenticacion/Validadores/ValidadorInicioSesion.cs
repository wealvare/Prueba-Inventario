using FluentValidation;
using Inventario.Aplicacion.Autenticacion.Dtos;

namespace Inventario.Aplicacion.Autenticacion.Validadores;

public sealed class ValidadorInicioSesion : AbstractValidator<SolicitudInicioSesion>
{
    public ValidadorInicioSesion()
    {
        RuleFor(solicitud => solicitud.NombreUsuario).NotEmpty().WithMessage("El nombre de usuario es obligatorio.");
        RuleFor(solicitud => solicitud.Contrasena).NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
