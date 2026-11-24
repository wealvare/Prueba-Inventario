using System.Net;
using System.Net.Http.Json;
using System.Text;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Pruebas.Api;

[Collection("Api")]
[Trait("Categoria", "Integracion")]
public sealed class ProductosApiPruebas(FabricaApi fabrica)
{
    private const string Ruta = "/api/productos";
    private static readonly DateOnly Fecha = new(2026, 1, 15);

    private readonly HttpClient _administrador = fabrica.ClienteCon(RolUsuario.Administrador);
    private readonly HttpClient _consulta = fabrica.ClienteCon(RolUsuario.Consulta);

    private static string Unico() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    private static SolicitudLote Lote(string numero, decimal precio = 10, int? id = null) => new(id, numero, Fecha, precio, 5);

    private static SolicitudCrearProducto Nuevo(string? codigo = null, string? categoria = null) =>
        new(codigo ?? Unico(), "Teclado", "Mecánico", categoria ?? "Periféricos", [Lote("L-1"), Lote("L-2", 20)]);

    private static SolicitudActualizarProducto Cambio(string codigo, params SolicitudLote[] lotes) =>
        new(codigo, "Ratón", null, "Accesorios", true, lotes);

    private async Task<ProductoDto> CrearAsync(SolicitudCrearProducto? solicitud = null)
    {
        var respuesta = await _administrador.PostAsJsonAsync(Ruta, solicitud ?? Nuevo());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<ProductoDto>())!;
    }

    [Fact]
    public async Task Crud_Completo()
    {
        var solicitud = Nuevo();
        var creacion = await _administrador.PostAsJsonAsync(Ruta, solicitud);
        var creado = (await creacion.Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        Assert.Equal($"{Ruta}/{creado.Id}", creacion.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.All(creado.Lotes, lote => Assert.True(lote.Id > 0));

        var leido = (await _consulta.GetFromJsonAsync<ProductoDto>(creacion.Headers.Location))!;
        Assert.Equal(solicitud.Codigo, leido.Codigo);
        Assert.Equal([10m, 20m], leido.Lotes.Select(lote => lote.Precio));

        var conservado = leido.Lotes[0];
        var edicion = await _administrador.PutAsJsonAsync(
            $"{Ruta}/{creado.Id}", Cambio(solicitud.Codigo, Lote("L-1", 15, conservado.Id), Lote("L-3", 30)));
        var editado = (await edicion.Content.ReadFromJsonAsync<ProductoDto>())!;
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        Assert.Equal("Ratón", editado.Nombre);
        Assert.Equal(["L-1", "L-3"], editado.Lotes.Select(lote => lote.NumeroLote));
        Assert.Equal((conservado.Id, 15m), (editado.Lotes[0].Id, editado.Lotes[0].Precio));

        var eliminacion = await _administrador.DeleteAsync($"{Ruta}/{creado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);
        Assert.False((await _consulta.GetFromJsonAsync<ProductoDto>($"{Ruta}/{creado.Id}"))!.Activo);
    }

    [Fact]
    public async Task Listar_AplicaValoresPorDefecto()
    {
        var pagina = (await _consulta.GetFromJsonAsync<ResultadoPaginado<ProductoResumenDto>>(Ruta))!;

        Assert.Equal((1, 10), (pagina.Pagina, pagina.TamanoPagina));
        Assert.Equal(10, pagina.Elementos.Count);
        Assert.All(pagina.Elementos, producto => Assert.True(producto.Activo));
    }

    [Fact]
    public async Task Listar_AplicaFiltrosYPaginacion()
    {
        var categoria = Unico();
        var activo = await CrearAsync(Nuevo(categoria: categoria));
        var inactivo = await CrearAsync(Nuevo(categoria: categoria));
        await _administrador.DeleteAsync($"{Ruta}/{inactivo.Id}");

        async Task<ResultadoPaginado<ProductoResumenDto>> Pagina(string filtros) =>
            (await _consulta.GetFromJsonAsync<ResultadoPaginado<ProductoResumenDto>>(
                $"{Ruta}?categoria={categoria}&{filtros}"))!;

        Assert.Equal(activo.Id, Assert.Single((await Pagina("")).Elementos).Id);
        Assert.Equal(inactivo.Id, Assert.Single((await Pagina("estado=inactivos")).Elementos).Id);
        Assert.Equal(2, (await Pagina("estado=todos")).TotalElementos);
        Assert.Equal(activo.Id, Assert.Single((await Pagina($"busqueda={activo.Codigo}&precioMinimo=20")).Elementos).Id);
        Assert.Empty((await Pagina("precioMinimo=20.5")).Elementos);
        var segunda = await Pagina("estado=todos&pagina=2&tamanoPagina=1");
        Assert.Equal((1, 2), (segunda.Elementos.Count, segunda.TotalPaginas));
    }

    [Fact]
    public async Task Consulta_AlEscribir_Recibe403()
    {
        var producto = await CrearAsync();

        HttpResponseMessage[] respuestas =
        [
            await _consulta.PostAsJsonAsync(Ruta, Nuevo()),
            await _consulta.PutAsJsonAsync($"{Ruta}/{producto.Id}", Cambio(producto.Codigo)),
            await _consulta.DeleteAsync($"{Ruta}/{producto.Id}")
        ];

        foreach (var respuesta in respuestas)
            Assert.Equal(
                "No tienes permiso para realizar esta acción.",
                (await respuesta.ProblemaAsync(HttpStatusCode.Forbidden)).Detalle());
        Assert.True((await _consulta.GetFromJsonAsync<ProductoDto>($"{Ruta}/{producto.Id}"))!.Activo);
    }

    [Fact]
    public async Task CodigoDuplicado_Devuelve409()
    {
        var existente = await CrearAsync();
        var otro = await CrearAsync();

        var creacion = await _administrador.PostAsJsonAsync(Ruta, Nuevo(existente.Codigo.ToLowerInvariant()));
        var edicion = await _administrador.PutAsJsonAsync($"{Ruta}/{otro.Id}", Cambio(existente.Codigo));

        var mensaje = $"Ya existe un producto con el código '{existente.Codigo}'.";
        Assert.Equal(mensaje, (await creacion.ProblemaAsync(HttpStatusCode.Conflict)).Detalle());
        Assert.Equal(mensaje, (await edicion.ProblemaAsync(HttpStatusCode.Conflict)).Detalle());
    }

    [Fact]
    public async Task CodigoDuplicado_EnParalelo_SoloCreaUno()
    {
        var solicitud = Nuevo();

        var respuestas = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => _administrador.PostAsJsonAsync(Ruta, solicitud)));

        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task LoteDuplicado_Devuelve409()
    {
        var respuesta = await _administrador.PostAsJsonAsync(Ruta, Nuevo() with { Lotes = [Lote("L-1"), Lote("l-1")] });

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.Conflict);
        Assert.Equal("Ya existe un lote con el número 'L-1' en el producto.", problema.Detalle());
    }

    [Fact]
    public async Task LoteInexistente_Devuelve409()
    {
        var producto = await CrearAsync();

        var respuesta = await _administrador.PutAsJsonAsync(
            $"{Ruta}/{producto.Id}", Cambio(producto.Codigo, Lote("L-9", id: int.MaxValue)));

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.Conflict);
        Assert.Equal(
            "Uno de los lotes ya no existe en el producto. Vuelve a cargarlo e inténtalo de nuevo.", problema.Detalle());
    }

    [Fact]
    public async Task ProductoInexistente_Devuelve404()
    {
        var ruta = $"{Ruta}/{int.MaxValue}";

        HttpResponseMessage[] respuestas =
        [
            await _consulta.GetAsync(ruta),
            await _administrador.PutAsJsonAsync(ruta, Cambio(Unico())),
            await _administrador.DeleteAsync(ruta)
        ];

        foreach (var respuesta in respuestas)
            Assert.Equal(
                $"No existe un producto con el id {int.MaxValue}.",
                (await respuesta.ProblemaAsync(HttpStatusCode.NotFound)).Detalle());
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_Devuelve400PorCampo()
    {
        var solicitud = new SolicitudCrearProducto("", "", null, "", [Lote("", 0)]);

        var respuesta = await _administrador.PostAsJsonAsync(Ruta, solicitud);

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["El código es obligatorio."], problema.Errores("codigo"));
        Assert.Equal(["El nombre es obligatorio."], problema.Errores("nombre"));
        Assert.Equal(["El número de lote es obligatorio."], problema.Errores("lotes[0].numeroLote"));
        Assert.Equal(["El precio debe ser mayor a cero."], problema.Errores("lotes[0].precio"));
    }

    [Theory]
    [InlineData("", "El cuerpo de la solicitud es obligatorio.")]
    [InlineData("null", "El cuerpo de la solicitud es obligatorio.")]
    [InlineData("{ no es json", "El formato del valor no es válido.")]
    [InlineData("""{ "lotes": [{ "precio": "abc" }] }""", "El formato del valor no es válido.")]
    [InlineData("""{ "lotes": [{ "fechaIngreso": "2026-13-45" }] }""", "El formato del valor no es válido.")]
    public async Task Crear_ConCuerpoInvalido_Devuelve400EnEspanol(string cuerpo, string mensaje)
    {
        var respuesta = await _administrador.PostAsync(Ruta, new StringContent(cuerpo, Encoding.UTF8, "application/json"));

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.BadRequest);
        var mensajes = problema.GetProperty("errors").EnumerateObject()
            .SelectMany(campo => campo.Value.EnumerateArray()).Select(error => error.GetString()!).ToArray();
        Assert.Equal([mensaje], mensajes);
    }

    [Theory]
    [InlineData("pagina=0", "pagina", "La página debe estar entre 1 y 1000000.")]
    [InlineData("tamanoPagina=101", "tamanoPagina", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("pagina=abc", "pagina", "El valor 'abc' no es válido.")]
    [InlineData("estado=otro", "estado", "El valor 'otro' no es válido.")]
    [InlineData("precioMinimo=20&precioMaximo=5", "precioMaximo", "El precio máximo no puede ser menor que el precio mínimo.")]
    public async Task Listar_ConFiltrosInvalidos_Devuelve400(string filtros, string campo, string mensaje)
    {
        var respuesta = await _consulta.GetAsync($"{Ruta}?{filtros}");

        Assert.Equal([mensaje], (await respuesta.ProblemaAsync(HttpStatusCode.BadRequest)).Errores(campo));
    }
}
