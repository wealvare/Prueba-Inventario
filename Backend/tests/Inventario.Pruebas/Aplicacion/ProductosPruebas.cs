using FluentValidation;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.CasosUso;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Salida;
using Inventario.Aplicacion.Productos.Validadores;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;
using NSubstitute;

namespace Inventario.Pruebas.Aplicacion;

public sealed class ProductosPruebas
{
    private static readonly SolicitudLote Lote = new(null, "L-1", new DateOnly(2026, 1, 15), 10, 5);
    private static readonly SolicitudCrearProducto Creacion = new("p-01", "Teclado", null, "Periféricos", [Lote]);
    private static readonly SolicitudActualizarProducto Actualizacion =
        new("p-02", "Ratón", "Inalámbrico", "Periféricos", false, [Lote]);

    private readonly IRepositorioProductos _repositorio = Substitute.For<IRepositorioProductos>();
    private readonly Producto _producto = new("P-01", "Teclado", null, "Periféricos");

    private CrearProducto Crear => new(_repositorio, new ValidadorCrearProducto());
    private ActualizarProducto Actualizar => new(_repositorio, new ValidadorActualizarProducto());
    private ListarProductos Listar => new(_repositorio, new ValidadorFiltroProductos());

    [Fact]
    public async Task Crear_ConDatosValidos_GuardaElProducto()
    {
        var resultado = await Crear.EjecutarAsync(Creacion, CancellationToken.None);

        Assert.Equal("P-01", resultado.Codigo);
        Assert.Equal("L-1", Assert.Single(resultado.Lotes).NumeroLote);
        _repositorio.Received(1).Agregar(Arg.Is<Producto>(producto => producto.Codigo == "P-01"));
        await _repositorio.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Crear_ConCodigoExistente_FallaSinGuardar()
    {
        _repositorio.ExisteCodigoAsync("P-01", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ExcepcionCodigoDuplicado>(() =>
            Crear.EjecutarAsync(Creacion, CancellationToken.None));

        Assert.Equal("Ya existe un producto con el código 'P-01'.", error.Message);
        await VerificarSinGuardar();
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_DevuelveMensajesEnEspanol()
    {
        var solicitud = new SolicitudCrearProducto(
            "", "", null, "", [Lote with { NumeroLote = "", Precio = 0, Cantidad = -1, FechaIngreso = default }]);

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(solicitud, CancellationToken.None));

        Assert.Equal(
            [
                "El código es obligatorio.",
                "El nombre es obligatorio.",
                "La categoría es obligatoria.",
                "El número de lote es obligatorio.",
                "La fecha de ingreso es obligatoria.",
                "El precio debe ser mayor a cero.",
                "La cantidad no puede ser negativa."
            ],
            error.Errors.Select(fallo => fallo.ErrorMessage));
        await VerificarSinGuardar();
    }

    [Fact]
    public async Task Crear_ConLotesNulos_FallaLaValidacion()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(Creacion with { Lotes = null! }, CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(Creacion with { Lotes = [null!] }, CancellationToken.None));
    }

    [Fact]
    public async Task Crear_ConPrecioDeMasDeDosDecimales_FallaLaValidacion() =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(Creacion with { Lotes = [Lote with { Precio = 1.234m }] }, CancellationToken.None));

    [Fact]
    public async Task Actualizar_ConDatosValidos_ModificaElProducto()
    {
        _repositorio.ObtenerParaEdicionAsync(7, Arg.Any<CancellationToken>()).Returns(_producto);

        var resultado = await Actualizar.EjecutarAsync(7, Actualizacion, CancellationToken.None);

        Assert.Equal("P-02", resultado.Codigo);
        Assert.Equal("Ratón", resultado.Nombre);
        Assert.False(resultado.Activo);
        Assert.Single(resultado.Lotes);
        await _repositorio.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Actualizar_ConCodigoDeOtroProducto_FallaSinModificar()
    {
        _repositorio.ObtenerParaEdicionAsync(7, Arg.Any<CancellationToken>()).Returns(_producto);
        _repositorio.ExisteCodigoAsync("P-02", 7, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ExcepcionCodigoDuplicado>(() =>
            Actualizar.EjecutarAsync(7, Actualizacion, CancellationToken.None));

        Assert.Equal("P-01", _producto.Codigo);
        await VerificarSinGuardar();
    }

    [Fact]
    public async Task Actualizar_ProductoInexistente_Falla() =>
        await Assert.ThrowsAsync<ExcepcionProductoNoEncontrado>(() =>
            Actualizar.EjecutarAsync(7, Actualizacion, CancellationToken.None));

    [Fact]
    public async Task Eliminar_DesactivaElProducto()
    {
        _repositorio.ObtenerParaEdicionAsync(7, Arg.Any<CancellationToken>()).Returns(_producto);

        await new EliminarProducto(_repositorio).EjecutarAsync(7, CancellationToken.None);

        Assert.False(_producto.Activo);
        await _repositorio.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Eliminar_ProductoInexistente_Falla()
    {
        await Assert.ThrowsAsync<ExcepcionProductoNoEncontrado>(() =>
            new EliminarProducto(_repositorio).EjecutarAsync(7, CancellationToken.None));
        await VerificarSinGuardar();
    }

    [Fact]
    public async Task Obtener_DevuelveElProducto()
    {
        _repositorio.ObtenerPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(_producto);

        var resultado = await new ObtenerProducto(_repositorio).EjecutarAsync(7, CancellationToken.None);

        Assert.Equal("Teclado", resultado.Nombre);
    }

    [Fact]
    public async Task Obtener_ProductoInexistente_Falla()
    {
        var error = await Assert.ThrowsAsync<ExcepcionProductoNoEncontrado>(() =>
            new ObtenerProducto(_repositorio).EjecutarAsync(7, CancellationToken.None));

        Assert.Equal("No existe un producto con el id 7.", error.Message);
    }

    [Fact]
    public async Task Listar_EnviaLosFiltrosAlRepositorio()
    {
        var filtro = new FiltroProductos(2, 10, "tec", "Periféricos", true, 5, 20);
        var pagina = new ResultadoPaginado<ProductoResumenDto>([], 2, 10, 21);
        _repositorio.ListarAsync(filtro, Arg.Any<CancellationToken>()).Returns(pagina);

        var resultado = await Listar.EjecutarAsync(filtro, CancellationToken.None);

        Assert.Same(pagina, resultado);
        Assert.Equal(3, resultado.TotalPaginas);
    }

    [Theory]
    [InlineData(0, 10, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 101, null, null)]
    [InlineData(1, 10, -1, null)]
    [InlineData(1, 10, null, -1)]
    [InlineData(1, 10, 20, 5)]
    public async Task Listar_ConFiltrosInvalidos_FallaLaValidacion(int pagina, int tamano, int? minimo, int? maximo) =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            Listar.EjecutarAsync(
                new FiltroProductos(pagina, tamano, PrecioMinimo: minimo, PrecioMaximo: maximo),
                CancellationToken.None));

    private async Task VerificarSinGuardar() =>
        await _repositorio.DidNotReceiveWithAnyArgs().GuardarCambiosAsync(default);
}
