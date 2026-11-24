using FluentValidation;
using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Validadores;

public sealed class ValidadorRegistro : AbstractValidator<SolicitudRegistro>
{
    public ValidadorRegistro()
    {
        RuleFor(solicitud => solicitud.NombreUsuario).NombreUsuarioValido();
        RuleFor(solicitud => solicitud.Correo).CorreoValido();
        RuleFor(solicitud => solicitud.Contrasena).ContrasenaValida();
    }
}
