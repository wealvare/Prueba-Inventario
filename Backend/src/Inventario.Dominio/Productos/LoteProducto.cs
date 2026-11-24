using Inventario.Dominio.Comun;
using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Productos;

public sealed class LoteProducto
{
    public const int LongitudNumeroLote = 50;

    internal LoteProducto(string numeroLote, DateOnly fechaIngreso, decimal precio, int cantidad)
    {
        NumeroLote = string.Empty;
        Actualizar(numeroLote, fechaIngreso, precio, cantidad);
    }

    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public string NumeroLote { get; private set; }
    public DateOnly FechaIngreso { get; private set; }
    public decimal Precio { get; private set; }
    public int Cantidad { get; private set; }

    public static LoteProducto Reconstruir(
        int id,
        int productoId,
        string numeroLote,
        DateOnly fechaIngreso,
        decimal precio,
        int cantidad) => new(numeroLote, fechaIngreso, precio, cantidad)
        {
            Id = id,
            ProductoId = productoId
        };

    internal void Actualizar(string numeroLote, DateOnly fechaIngreso, decimal precio, int cantidad)
    {
        var numero = Texto.Requerido(numeroLote, LongitudNumeroLote, "número de lote");

        if (fechaIngreso == default)
            throw new ExcepcionReglaNegocio("La fecha de ingreso del lote es obligatoria.");

        if (precio <= 0)
            throw new ExcepcionReglaNegocio("El precio del lote debe ser mayor a cero.");

        if (cantidad < 0)
            throw new ExcepcionReglaNegocio("La cantidad del lote no puede ser negativa.");

        NumeroLote = numero;
        FechaIngreso = fechaIngreso;
        Precio = precio;
        Cantidad = cantidad;
    }
}
