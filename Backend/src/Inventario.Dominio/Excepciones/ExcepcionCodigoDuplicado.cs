namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionCodigoDuplicado(string codigo)
    : ExcepcionDominio($"Ya existe un producto con el código '{codigo}'.");
