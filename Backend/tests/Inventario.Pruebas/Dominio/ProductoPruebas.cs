using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;

namespace Inventario.Pruebas.Dominio;

public sealed class ProductoPruebas
{
    private static readonly DateOnly Fecha = new(2026, 1, 15);

    private static Producto Crear() => new("P-01", "Teclado", "Mecánico", "Periféricos");

    private static DatosLote Lote(string numero = "L-1", decimal precio = 10, int cantidad = 5, int? id = null) =>
        new(id, numero, Fecha, precio, cantidad);

    [Fact]
    public void Crear_NormalizaCodigoYQuedaActivo()
    {
        var producto = new Producto(" p-01 ", " Teclado ", null, "Periféricos");

        Assert.Equal("P-01", producto.Codigo);
        Assert.Equal("Teclado", producto.Nombre);
        Assert.Equal(string.Empty, producto.Descripcion);
        Assert.True(producto.Activo);
    }

    [Theory]
    [InlineData("", "Teclado", "Periféricos")]
    [InlineData("P-01", " ", "Periféricos")]
    [InlineData("P-01", "Teclado", null)]
    public void Crear_SinDatosObligatorios_Falla(string codigo, string nombre, string? categoria) =>
        Assert.Throws<ExcepcionReglaNegocio>(() => new Producto(codigo, nombre, null, categoria!));

    [Fact]
    public void Crear_ConCodigoDemasiadoLargo_Falla() =>
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            new Producto(new string('A', Producto.LongitudCodigo + 1), "Teclado", null, "Periféricos"));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Lote_ConPrecioNoPositivo_Falla(int precio) =>
        Assert.Throws<ExcepcionReglaNegocio>(() => Crear().SincronizarLotes([Lote(precio: precio)]));

    [Fact]
    public void Lote_ConCantidadNegativa_Falla() =>
        Assert.Throws<ExcepcionReglaNegocio>(() => Crear().SincronizarLotes([Lote(cantidad: -1)]));

    [Fact]
    public void Lote_ConCantidadCero_EsValido()
    {
        var producto = Crear();

        producto.SincronizarLotes([Lote(cantidad: 0)]);

        Assert.Equal(0, Assert.Single(producto.Lotes).Cantidad);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Lote_SinNumero_Falla(string? numero) =>
        Assert.Throws<ExcepcionReglaNegocio>(() => Crear().SincronizarLotes([Lote(numero!)]));

    [Fact]
    public void Lote_SinFechaIngreso_Falla() =>
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            Crear().SincronizarLotes([new DatosLote(null, "L-1", default, 10, 1)]));

    [Fact]
    public void Lotes_AdmitenDistintoPrecio()
    {
        var producto = Crear();

        producto.SincronizarLotes([Lote("L-1", 10), Lote("L-2", 12.5m)]);

        Assert.Equal([10m, 12.5m], producto.Lotes.Select(lote => lote.Precio));
    }

    [Fact]
    public void Lotes_ConNumeroRepetido_Fallan() =>
        Assert.Throws<ExcepcionReglaNegocio>(() => Crear().SincronizarLotes([Lote("L-1"), Lote(" l-1 ")]));

    [Fact]
    public void Sincronizar_ActualizaAgregaYQuitaLotes()
    {
        var producto = Crear();
        producto.SincronizarLotes([Lote("L-1"), Lote("L-2")]);
        Identidad.Asignar(producto.Lotes.ElementAt(0), 1);
        Identidad.Asignar(producto.Lotes.ElementAt(1), 2);

        producto.SincronizarLotes([Lote("L-1B", 20, 8, 1), Lote("L-3")]);

        Assert.Equal(["L-1B", "L-3"], producto.Lotes.Select(lote => lote.NumeroLote));
        Assert.Equal(20, producto.Lotes.First().Precio);
    }

    [Fact]
    public void Sincronizar_ConLoteAjeno_FallaYConservaLotes()
    {
        var producto = Crear();
        producto.SincronizarLotes([Lote()]);

        Assert.Throws<ExcepcionReglaNegocio>(() => producto.SincronizarLotes([Lote("L-9", id: 99)]));
        Assert.Single(producto.Lotes);
    }

    [Fact]
    public void Sincronizar_ConIdRepetido_Falla()
    {
        var producto = Crear();
        producto.SincronizarLotes([Lote()]);
        Identidad.Asignar(producto.Lotes.First(), 1);

        Assert.Throws<ExcepcionReglaNegocio>(() =>
            producto.SincronizarLotes([Lote("L-1", id: 1), Lote("L-2", id: 1)]));
    }

    [Fact]
    public void Desactivar_DejaElProductoInactivo()
    {
        var producto = Crear();

        producto.Desactivar();

        Assert.False(producto.Activo);
    }
}
