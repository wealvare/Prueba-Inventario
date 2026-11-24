using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Aplicacion.Productos.Dtos;
using Inventario.Aplicacion.Productos.Puertos.Entrada;
using Inventario.Dominio.Usuarios;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog.Events;

namespace Inventario.Pruebas.Api;

[Collection("Api")]
[Trait("Categoria", "Integracion")]
public sealed class CanalizacionPruebas(FabricaApi fabrica)
{
    [Fact]
    public async Task ErrorNoControlado_Devuelve500SinDetallesYLoRegistra()
    {
        var casoUso = Substitute.For<IObtenerProducto>();
        casoUso.EjecutarAsync(default, default).ThrowsAsyncForAnyArgs(new InvalidOperationException("detalle interno"));
        await using var conError = fabrica.WithWebHostBuilder(constructor =>
            constructor.ConfigureTestServices(servicios => servicios.AddScoped(_ => casoUso)));
        using var cliente = conError.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", fabrica.Token(RolUsuario.Consulta));
        cliente.DefaultRequestHeaders.Add("X-Correlacion", "prueba-error-500");

        var respuesta = await cliente.GetAsync("/api/productos/1");

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.InternalServerError);
        Assert.Equal("Ocurrió un error inesperado. Inténtalo de nuevo más tarde.", problema.Detalle());
        Assert.DoesNotContain("detalle interno", problema.GetRawText());
        Assert.Contains(fabrica.Registros.Eventos, evento =>
            evento.Level == LogEventLevel.Error
            && evento.Exception?.Message == "detalle interno"
            && evento.Properties["Correlacion"].ToString() == "\"prueba-error-500\"");
    }

    [Theory]
    [InlineData("correlacion-1", true)]
    [InlineData("valor no válido!", false)]
    [InlineData("", false)]
    public async Task Correlacion_SoloConservaValoresSeguros(string enviada, bool conserva)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, "/salud");
        solicitud.Headers.TryAddWithoutValidation("X-Correlacion", Uri.EscapeDataString(enviada));

        var respuesta = await fabrica.CreateClient().SendAsync(solicitud);

        Assert.Equal(conserva, Assert.Single(respuesta.Headers.GetValues("X-Correlacion")) == enviada);
    }

    [Fact]
    public async Task Registros_NoContienenContrasenasNiTokens()
    {
        var cliente = fabrica.CreateClient();
        await cliente.PostAsJsonAsync(
            "/api/autenticacion/login", new { nombreUsuario = "admin", contrasena = FabricaApi.ContrasenaAdministrador });
        await cliente.PostAsJsonAsync(
            "/api/autenticacion/login", new { nombreUsuario = "admin", contrasena = "contrasena-equivocada" });
        await fabrica.ClienteCon(RolUsuario.Administrador).GetAsync("/api/productos");
        await fabrica.ClienteCon("token-no-valido").GetAsync("/api/productos");

        var texto = string.Join('\n', fabrica.Registros.Eventos.Select(evento =>
            $"{evento.RenderMessage()} {string.Join(' ', evento.Properties.Values)} {evento.Exception}"));

        Assert.Contains("/api/autenticacion/login", texto);
        Assert.DoesNotContain(FabricaApi.ContrasenaAdministrador, texto);
        Assert.DoesNotContain("contrasena-equivocada", texto);
        Assert.DoesNotContain(fabrica.Token(RolUsuario.Administrador), texto);
        Assert.DoesNotContain("token-no-valido", texto);
    }

    [Fact]
    public async Task Salud_EsPublicaYVerificaLaBaseDeDatos()
    {
        var respuesta = await fabrica.CreateClient().GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Healthy", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(FabricaApi.OrigenPermitido, true)]
    [InlineData("http://otro-origen.local", false)]
    public async Task Cors_SoloPermiteLosOrigenesConfigurados(string origen, bool permitido)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Options, "/api/productos");
        solicitud.Headers.Add("Origin", origen);
        solicitud.Headers.Add("Access-Control-Request-Method", "POST");
        solicitud.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var respuesta = await fabrica.CreateClient().SendAsync(solicitud);

        Assert.Equal(permitido, respuesta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_SeIncluyeEnLasRespuestasDeError()
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/productos");
        solicitud.Headers.Add("Origin", FabricaApi.OrigenPermitido);

        var respuesta = await fabrica.CreateClient().SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains(FabricaApi.OrigenPermitido, respuesta.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Respuestas_SeComprimenSiElClienteLoAdmite()
    {
        var cliente = fabrica.ClienteCon(RolUsuario.Consulta);
        cliente.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");

        var respuesta = await cliente.GetAsync("/api/productos");

        Assert.Contains("gzip", respuesta.Content.Headers.ContentEncoding);
    }

    [Theory]
    [InlineData("GET", "/ruta-inexistente", HttpStatusCode.NotFound)]
    [InlineData("PATCH", "/api/productos", HttpStatusCode.MethodNotAllowed)]
    public async Task RutasNoAdmitidas_DevuelvenProblema(string metodo, string ruta, HttpStatusCode estado)
    {
        var respuesta = await fabrica.ClienteCon(RolUsuario.Administrador)
            .SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta));

        await respuesta.ProblemaAsync(estado);
    }

    [Fact]
    public async Task Swagger_DocumentaSeguridadEjemplosYRespuestas()
    {
        var documento = JsonDocument.Parse(await fabrica.CreateClient().GetStringAsync("/swagger/v1/swagger.json")).RootElement;
        var creacion = documento.GetProperty("paths").GetProperty("/api/productos").GetProperty("post");
        var login = documento.GetProperty("paths").GetProperty("/api/autenticacion/login").GetProperty("post");
        var componentes = documento.GetProperty("components");

        Assert.Equal("bearer", componentes.GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        Assert.True(creacion.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.False(login.TryGetProperty("security", out _));
        Assert.Equal(["201", "400", "401", "403", "409"],
            creacion.GetProperty("responses").EnumerateObject().Select(respuesta => respuesta.Name).Order());
        Assert.True(componentes.GetProperty("schemas").GetProperty(nameof(SolicitudCrearProducto)).TryGetProperty("examples", out _));
        Assert.Equal("Crea un producto", creacion.GetProperty("summary").GetString());
    }
}
