using FluentValidation;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.Validadores;

public sealed class ValidadorLote : AbstractValidator<SolicitudLote>
{
    public ValidadorLote()
    {
        RuleFor(lote => lote.NumeroLote)
            .NotEmpty().WithMessage("El número de lote es obligatorio.")
            .MaximumLength(LoteProducto.LongitudNumeroLote)
            .WithMessage($"El número de lote no puede superar los {LoteProducto.LongitudNumeroLote} caracteres.");

        RuleFor(lote => lote.FechaIngreso).NotEmpty().WithMessage("La fecha de ingreso es obligatoria.");

        RuleFor(lote => lote.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a cero.")
            .PrecisionScale(18, 2, true).WithMessage("El precio admite máximo 16 enteros y 2 decimales.");

        RuleFor(lote => lote.Cantidad).GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa.");
    }
}
