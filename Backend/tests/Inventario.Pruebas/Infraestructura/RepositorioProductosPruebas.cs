using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Productos;

namespace Inventario.Pruebas.Infraestructura;

[Collection("BaseDatos")]
[Trait("Categoria", "Integracion")]
public sealed class RepositorioProductosPruebas(BaseDatosFixture baseDatos)
{
    private static readonly DateOnly Fecha = new(2026, 1, 15);

    private static string Unico() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    private static DatosLote Lote(string numero, decimal precio = 10, int cantidad = 5, int? id = null) =>
        new(id, numero, Fecha, precio, cantidad);

    private async Task<Producto> GuardarAsync(
        string categoria,
        string nombre = "Teclado",
        bool activo = true,
        params DatosLote[] lotes)
    {
        var repositorio = baseDatos.CrearRepositorio();
        var producto = new Producto(Unico(), nombre, "Descripción", categoria);
        producto.SincronizarLotes(lotes);

        if (!activo)
            producto.Desactivar();

        repositorio.Agregar(producto);
        await repositorio.GuardarCambiosAsync(default);
        return producto;
    }

    private async Task<Producto> LeerAsync(int id) =>
        (await baseDatos.CrearRepositorio().ObtenerPorIdAsync(id, default))!;

    private async Task<Producto> EditarAsync(int id, Action<Producto> cambio)
    {
        var repositorio = baseDatos.CrearRepositorio();
        var producto = (await repositorio.ObtenerParaEdicionAsync(id, default))!;
        cambio(producto);
        await repositorio.GuardarCambiosAsync(default);
        return producto;
    }

    private async Task<int[]> IdsAsync(FiltroProductos filtro) =>
        (await baseDatos.CrearRepositorio().ListarAsync(filtro, default)).Elementos.Select(p => p.Id).ToArray();

    [Fact]
    public async Task Guardar_AsignaIdsYConservaLosDatos()
    {
        var guardado = await GuardarAsync(Unico(), activo: false, lotes: [Lote("L-1", 12.34m, 7)]);

        var leido = await LeerAsync(guardado.Id);

        var lote = Assert.Single(leido.Lotes);
        Assert.Equal((guardado.Codigo, "Teclado", "Descripción", false),
            (leido.Codigo, leido.Nombre, leido.Descripcion, leido.Activo));
        Assert.Equal((12.34m, 7, Fecha, guardado.Id), (lote.Precio, lote.Cantidad, lote.FechaIngreso, lote.ProductoId));
        Assert.Equal(lote.Id, Assert.Single(guardado.Lotes).Id);
    }

    [Fact]
    public async Task Obtener_ProductoInexistente_DevuelveNulo() =>
        Assert.Null(await baseDatos.CrearRepositorio().ObtenerPorIdAsync(int.MaxValue, default));

    [Fact]
    public async Task Guardar_ConCodigoDuplicado_LanzaExcepcionDeDominio()
    {
        var existente = await GuardarAsync(Unico());
        var repositorio = baseDatos.CrearRepositorio();
        repositorio.Agregar(new Producto(existente.Codigo, "Otro", null, "Otra"));

        var error = await Assert.ThrowsAsync<ExcepcionCodigoDuplicado>(() => repositorio.GuardarCambiosAsync(default));

        Assert.Equal($"Ya existe un producto con el código '{existente.Codigo}'.", error.Message);
    }

    [Fact]
    public async Task Guardar_SiFallaUnProducto_RevierteTodos()
    {
        var existente = await GuardarAsync(Unico());
        var repositorio = baseDatos.CrearRepositorio();
        var valido = new Producto(Unico(), "Válido", null, "Otra");
        valido.SincronizarLotes([Lote("L-1")]);
        repositorio.Agregar(valido);
        repositorio.Agregar(new Producto(existente.Codigo, "Otro", null, "Otra"));

        await Assert.ThrowsAsync<ExcepcionCodigoDuplicado>(() => repositorio.GuardarCambiosAsync(default));

        Assert.False(await repositorio.ExisteCodigoAsync(valido.Codigo, null, default));
        Assert.Equal(0, valido.Id);
    }

    [Fact]
    public async Task Actualizar_ConCodigoDuplicado_LanzaExcepcionDeDominio()
    {
        var primero = await GuardarAsync(Unico());
        var segundo = await GuardarAsync(Unico());

        await Assert.ThrowsAsync<ExcepcionCodigoDuplicado>(() =>
            EditarAsync(segundo.Id, p => p.Actualizar(primero.Codigo, "Otro", null, "Otra", true)));
    }

    [Fact]
    public async Task Actualizar_ModificaElProducto()
    {
        var producto = await GuardarAsync(Unico());
        var codigo = Unico();

        await EditarAsync(producto.Id, p => p.Actualizar(codigo, "Nuevo", "Otra descripción", "Otra", false));

        var leido = await LeerAsync(producto.Id);
        Assert.Equal((codigo, "Nuevo", "Otra descripción", "Otra", false),
            (leido.Codigo, leido.Nombre, leido.Descripcion, leido.Categoria, leido.Activo));
    }

    [Fact]
    public async Task Actualizar_SincronizaLotesPorId()
    {
        var producto = await GuardarAsync(Unico(), lotes: [Lote("L-1"), Lote("L-2")]);
        var conservado = producto.Lotes.First(lote => lote.NumeroLote == "L-1");
        var eliminado = producto.Lotes.First(lote => lote.NumeroLote == "L-2");

        var editado = await EditarAsync(producto.Id, p => p.SincronizarLotes(
            [new DatosLote(conservado.Id, "L-1B", new DateOnly(2026, 3, 1), 99.55m, 3), Lote("L-3")]));

        var lotes = (await LeerAsync(producto.Id)).Lotes;
        var actualizado = lotes.Single(lote => lote.Id == conservado.Id);
        Assert.Equal(2, lotes.Count);
        Assert.Equal(("L-1B", new DateOnly(2026, 3, 1), 99.55m, 3),
            (actualizado.NumeroLote, actualizado.FechaIngreso, actualizado.Precio, actualizado.Cantidad));
        Assert.DoesNotContain(lotes, lote => lote.Id == eliminado.Id);
        Assert.Equal(editado.Lotes.Single(lote => lote.NumeroLote == "L-3").Id,
            lotes.Single(lote => lote.NumeroLote == "L-3").Id);
    }

    [Fact]
    public async Task Actualizar_PuedeIntercambiarNumerosDeLote()
    {
        var producto = await GuardarAsync(Unico(), lotes: [Lote("L-1"), Lote("L-2")]);
        var ids = producto.Lotes.ToDictionary(lote => lote.NumeroLote, lote => lote.Id);

        await EditarAsync(producto.Id, p =>
            p.SincronizarLotes([Lote("L-2", id: ids["L-1"]), Lote("L-1", id: ids["L-2"])]));

        Assert.Equal("L-2", (await LeerAsync(producto.Id)).Lotes.Single(lote => lote.Id == ids["L-1"]).NumeroLote);
    }

    [Fact]
    public async Task Actualizar_PuedeReutilizarElNumeroDeUnLoteEliminado()
    {
        var producto = await GuardarAsync(Unico(), lotes: [Lote("L-1")]);

        await EditarAsync(producto.Id, p => p.SincronizarLotes([Lote("L-1", 20)]));

        Assert.Equal(20, Assert.Single((await LeerAsync(producto.Id)).Lotes).Precio);
    }

    [Fact]
    public async Task Actualizar_ConLoteDuplicadoEnParalelo_FallaYRevierteTodo()
    {
        var producto = await GuardarAsync(Unico());
        var repositorio = baseDatos.CrearRepositorio();
        var desactualizado = (await repositorio.ObtenerParaEdicionAsync(producto.Id, default))!;
        await EditarAsync(producto.Id, p => p.SincronizarLotes([Lote("L-1", 10)]));
        desactualizado.Actualizar(producto.Codigo, "No debe guardarse", null, "Otra", true);
        desactualizado.SincronizarLotes([Lote("L-1", 20)]);

        var error = await Assert.ThrowsAsync<ExcepcionLoteDuplicado>(() => repositorio.GuardarCambiosAsync(default));

        var leido = await LeerAsync(producto.Id);
        Assert.Equal("Ya existe un lote con el número 'L-1' en el producto.", error.Message);
        Assert.Equal("Teclado", leido.Nombre);
        Assert.Equal(10, Assert.Single(leido.Lotes).Precio);
    }

    [Fact]
    public async Task Actualizar_ConLoteEliminadoEnParalelo_Falla()
    {
        var producto = await GuardarAsync(Unico(), lotes: [Lote("L-1")]);
        var idLote = producto.Lotes.First().Id;
        var repositorio = baseDatos.CrearRepositorio();
        var desactualizado = (await repositorio.ObtenerParaEdicionAsync(producto.Id, default))!;
        await EditarAsync(producto.Id, p => p.SincronizarLotes([]));
        desactualizado.SincronizarLotes([Lote("L-1", 20, id: idLote)]);

        await Assert.ThrowsAsync<ExcepcionReglaNegocio>(() => repositorio.GuardarCambiosAsync(default));
    }

    [Fact]
    public async Task MismoNumeroDeLote_EnProductosDistintos_EsValido()
    {
        await GuardarAsync(Unico(), lotes: [Lote("L-COMUN")]);
        var otro = await GuardarAsync(Unico(), lotes: [Lote("L-COMUN")]);

        Assert.Single((await LeerAsync(otro.Id)).Lotes);
    }

    [Fact]
    public async Task ExisteCodigo_PuedeExcluirElPropioProducto()
    {
        var producto = await GuardarAsync(Unico());
        var repositorio = baseDatos.CrearRepositorio();

        Assert.True(await repositorio.ExisteCodigoAsync(producto.Codigo, null, default));
        Assert.False(await repositorio.ExisteCodigoAsync(producto.Codigo, producto.Id, default));
    }

    [Fact]
    public async Task Listar_FiltraPorEstado()
    {
        var categoria = Unico();
        var activo = await GuardarAsync(categoria);
        var inactivo = await GuardarAsync(categoria, activo: false);

        Assert.Equal([activo.Id], await IdsAsync(new(Categoria: categoria)));
        Assert.Equal([activo.Id], await IdsAsync(new(Categoria: categoria, Estado: EstadoProductos.Activos)));
        Assert.Equal([inactivo.Id], await IdsAsync(new(Categoria: categoria, Estado: EstadoProductos.Inactivos)));
        Assert.Equal([activo.Id, inactivo.Id], await IdsAsync(new(Categoria: categoria, Estado: EstadoProductos.Todos)));
    }

    [Fact]
    public async Task Listar_PaginaOrdenaYResumeLotes()
    {
        var categoria = Unico();
        await GuardarAsync(categoria, "C");
        await GuardarAsync(categoria, "A", lotes: [Lote("L-1", 5, int.MaxValue), Lote("L-2", 9.5m, int.MaxValue)]);
        await GuardarAsync(categoria, "B");
        var repositorio = baseDatos.CrearRepositorio();

        var primera = await repositorio.ListarAsync(new(1, 2, Categoria: categoria), default);
        var segunda = await repositorio.ListarAsync(new(2, 2, Categoria: categoria), default);

        Assert.Equal((3, 2), (primera.TotalElementos, primera.TotalPaginas));
        Assert.Equal(["A", "B"], primera.Elementos.Select(producto => producto.Nombre));
        Assert.Equal("C", Assert.Single(segunda.Elementos).Nombre);
        var resumen = primera.Elementos[0];
        Assert.Equal((2, 2L * int.MaxValue, 5m, 9.5m),
            (resumen.CantidadLotes, resumen.CantidadTotal, resumen.PrecioMinimo, resumen.PrecioMaximo));
        Assert.Equal((0, 0L, null), (primera.Elementos[1].CantidadLotes, primera.Elementos[1].CantidadTotal, primera.Elementos[1].PrecioMinimo));
    }

    [Fact]
    public async Task Listar_FiltraPorBusquedaYPrecio()
    {
        var categoria = Unico();
        var barato = await GuardarAsync(categoria, "Ratón 100%", lotes: [Lote("L-1", 5), Lote("L-2", 50)]);
        var caro = await GuardarAsync(categoria, "Monitor", lotes: [Lote("L-1", 500)]);

        Assert.Equal([barato.Id], await IdsAsync(new(Categoria: categoria, Busqueda: "100%")));
        Assert.Equal([caro.Id], await IdsAsync(new(Categoria: categoria, Busqueda: caro.Codigo.ToLowerInvariant())));
        Assert.Equal([barato.Id], await IdsAsync(new(Categoria: categoria, PrecioMaximo: 10)));
        Assert.Equal([caro.Id], await IdsAsync(new(Categoria: categoria, PrecioMinimo: 100)));
        Assert.Empty(await IdsAsync(new(Categoria: categoria, PrecioMinimo: 10, PrecioMaximo: 40)));
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("' OR 1=1 --")]
    [InlineData("'; DROP TABLE dbo.LotesProducto; --")]
    public async Task Listar_TrataLaBusquedaComoTexto(string busqueda)
    {
        var categoria = Unico();
        var producto = await GuardarAsync(categoria, lotes: [Lote("L-1")]);

        Assert.Empty(await IdsAsync(new(Categoria: categoria, Busqueda: busqueda)));
        Assert.Empty(await IdsAsync(new(Categoria: busqueda)));
        Assert.Single((await LeerAsync(producto.Id)).Lotes);
    }
}
