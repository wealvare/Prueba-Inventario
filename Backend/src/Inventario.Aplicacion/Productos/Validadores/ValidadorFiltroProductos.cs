using FluentValidation;
using Inventario.Aplicacion.Productos.Dtos;

namespace Inventario.Aplicacion.Productos.Validadores;

public sealed class ValidadorFiltroProductos : AbstractValidator<FiltroProductos>
{
    private const int TamanoMaximo = 100;
    private const int PaginaMaxima = 1_000_000;

    public ValidadorFiltroProductos()
    {
        RuleFor(filtro => filtro.Pagina)
            .InclusiveBetween(1, PaginaMaxima).WithMessage($"La página debe estar entre 1 y {PaginaMaxima}.");

        RuleFor(filtro => filtro.TamanoPagina)
            .InclusiveBetween(1, TamanoMaximo).WithMessage($"El tamaño de página debe estar entre 1 y {TamanoMaximo}.");

        RuleFor(filtro => filtro.Estado).IsInEnum().WithMessage("El estado debe ser activos, inactivos o todos.");

        RuleFor(filtro => filtro.PrecioMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mínimo no puede ser negativo.");

        RuleFor(filtro => filtro.PrecioMaximo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio máximo no puede ser negativo.");

        RuleFor(filtro => filtro.PrecioMaximo)
            .Must((filtro, maximo) => maximo >= filtro.PrecioMinimo)
            .When(filtro => filtro.PrecioMinimo.HasValue && filtro.PrecioMaximo.HasValue)
            .WithMessage("El precio máximo no puede ser menor que el precio mínimo.");
    }
}
