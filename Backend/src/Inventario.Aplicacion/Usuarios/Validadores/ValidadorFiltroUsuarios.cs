using FluentValidation;
using Inventario.Aplicacion.Usuarios.Dtos;

namespace Inventario.Aplicacion.Usuarios.Validadores;

public sealed class ValidadorFiltroUsuarios : AbstractValidator<FiltroUsuarios>
{
    private const int TamanoMaximo = 100;
    private const int PaginaMaxima = 1_000_000;

    public ValidadorFiltroUsuarios()
    {
        RuleFor(filtro => filtro.Pagina)
            .InclusiveBetween(1, PaginaMaxima).WithMessage($"La página debe estar entre 1 y {PaginaMaxima}.");
        RuleFor(filtro => filtro.TamanoPagina)
            .InclusiveBetween(1, TamanoMaximo).WithMessage($"El tamaño de página debe estar entre 1 y {TamanoMaximo}.");
    }
}
