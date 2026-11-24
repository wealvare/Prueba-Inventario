using System.Text.RegularExpressions;
using FluentValidation;
using Inventario.Aplicacion.RegistrosCliente.Dtos;

namespace Inventario.Aplicacion.RegistrosCliente.Validadores;

public sealed partial class ValidadorRegistroCliente : AbstractValidator<SolicitudRegistroCliente>
{
    public const int LongitudMensaje = 500;
    public const int LongitudRuta = 200;

    public ValidadorRegistroCliente()
    {
        RuleFor(solicitud => solicitud.Nivel)
            .NotNull().WithMessage("El nivel es obligatorio.")
            .IsInEnum().WithMessage("El nivel debe ser Advertencia o Error.");
        RuleFor(solicitud => solicitud.Mensaje)
            .NotEmpty().WithMessage("El mensaje es obligatorio.")
            .MaximumLength(LongitudMensaje).WithMessage($"El mensaje admite hasta {LongitudMensaje} caracteres.");
        RuleFor(solicitud => solicitud.Ruta)
            .NotEmpty().WithMessage("La ruta es obligatoria.")
            .MaximumLength(LongitudRuta).WithMessage($"La ruta admite hasta {LongitudRuta} caracteres.")
            .Matches(FormatoRuta()).WithMessage("La ruta debe empezar con / y no llevar parámetros.");
        RuleFor(solicitud => solicitud.Tipo)
            .Matches(FormatoTipo()).WithMessage("El tipo debe ser un identificador de hasta 60 caracteres.");
        RuleFor(solicitud => solicitud.Correlacion)
            .Matches(FormatoCorrelacion()).WithMessage("La correlación admite hasta 64 letras, números o guiones.");
    }

    [GeneratedRegex(@"\A/[^?#\s]*\z")]
    private static partial Regex FormatoRuta();

    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9_]{0,59}\z")]
    private static partial Regex FormatoTipo();

    [GeneratedRegex(@"\A[A-Za-z0-9-]{1,64}\z")]
    private static partial Regex FormatoCorrelacion();
}
