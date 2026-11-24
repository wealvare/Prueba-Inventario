using FluentValidation;
using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Validadores;

public sealed class ValidadorCrearProducto : AbstractValidator<SolicitudCrearProducto>
{
    public ValidadorCrearProducto()
    {
        RuleFor(solicitud => solicitud.Codigo).CodigoValido();
        RuleFor(solicitud => solicitud.Nombre).NombreValido();
        RuleFor(solicitud => solicitud.Descripcion).DescripcionValida();
        RuleFor(solicitud => solicitud.Categoria).CategoriaValida();
        RuleFor(solicitud => solicitud.Lotes).NotNull().WithMessage("La lista de lotes es obligatoria.");
        RuleForEach(solicitud => solicitud.Lotes)
            .NotNull().WithMessage("El lote no puede ser nulo.")
            .SetValidator(new ValidadorLote());
    }
}
