using FluentValidation;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Validadores;

internal static class ReglasUsuario
{
    public static void NombreUsuarioValido<T>(this IRuleBuilderInitial<T, string> regla) => regla
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
        .MaximumLength(Usuario.LongitudNombreUsuario)
        .WithMessage($"El nombre de usuario no puede superar los {Usuario.LongitudNombreUsuario} caracteres.")
        .Must(Usuario.EsNombreUsuarioValido).WithMessage(Usuario.MensajeNombreUsuario);

    public static void CorreoValido<T>(this IRuleBuilderInitial<T, string> regla) => regla
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("El correo es obligatorio.")
        .MaximumLength(Usuario.LongitudCorreo)
        .WithMessage($"El correo no puede superar los {Usuario.LongitudCorreo} caracteres.")
        .Must(Usuario.EsCorreoValido).WithMessage(Usuario.MensajeCorreo);

    public static void ContrasenaValida<T>(this IRuleBuilderInitial<T, string> regla) => regla
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("La contraseña es obligatoria.")
        .Must(PoliticaContrasena.Cumple).WithMessage(PoliticaContrasena.Mensaje);

    public static void RolValido<T>(this IRuleBuilderInitial<T, RolUsuario?> regla) => regla
        .Cascade(CascadeMode.Stop)
        .NotNull().WithMessage("El rol es obligatorio.")
        .IsInEnum().WithMessage("El rol debe ser Administrador o Consulta.");
}
