using FluentValidation;
using Inventario.Dominio.Productos;

namespace Inventario.Aplicacion.Productos.Validadores;

internal static class ReglasProducto
{
    public static void CodigoValido<T>(this IRuleBuilder<T, string> regla) => regla
        .NotEmpty().WithMessage("El código es obligatorio.")
        .MaximumLength(Producto.LongitudCodigo)
        .WithMessage($"El código no puede superar los {Producto.LongitudCodigo} caracteres.");

    public static void NombreValido<T>(this IRuleBuilder<T, string> regla) => regla
        .NotEmpty().WithMessage("El nombre es obligatorio.")
        .MaximumLength(Producto.LongitudNombre)
        .WithMessage($"El nombre no puede superar los {Producto.LongitudNombre} caracteres.");

    public static void DescripcionValida<T>(this IRuleBuilder<T, string?> regla) => regla
        .MaximumLength(Producto.LongitudDescripcion)
        .WithMessage($"La descripción no puede superar los {Producto.LongitudDescripcion} caracteres.");

    public static void CategoriaValida<T>(this IRuleBuilder<T, string> regla) => regla
        .NotEmpty().WithMessage("La categoría es obligatoria.")
        .MaximumLength(Producto.LongitudCategoria)
        .WithMessage($"La categoría no puede superar los {Producto.LongitudCategoria} caracteres.");
}
