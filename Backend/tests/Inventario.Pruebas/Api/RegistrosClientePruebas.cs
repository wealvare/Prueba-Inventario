using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Inventario.Aplicacion.RegistrosCliente.Dtos;
using Microsoft.AspNetCore.Hosting;
using Serilog.Events;

namespace Inventario.Pruebas.Api;

[Collection("Api")]
[Trait("Categoria", "Integracion")]
public sealed class RegistrosClientePruebas(FabricaApi fabrica)
{
    private const string Ruta = "/api/registros-cliente";

    private static object Solicitud(
        string correlacion,
        string mensaje = "No cargó la pantalla",
        string nivel = "Error") =>
        new { nivel, mensaje, ruta = "/productos/5", tipo = "TypeError", correlacion };

    private LogEvent Registrado(string correlacion) => Assert.Single(fabrica.Registros.Eventos, evento =>
        evento.Properties.GetValueOrDefault("Correlacion")?.ToString() == $"\"{correlacion}\""
        && evento.Properties.ContainsKey("MensajeCliente"));

    [Theory]
    [InlineData("Error", LogEventLevel.Error)]
    [InlineData("Advertencia", LogEventLevel.Warning)]
    public async Task Registrar_SinSesion_EscribeNivelOrigenCorrelacionYRuta(string nivel, LogEventLevel esperado)
    {
        var correlacion = Guid.NewGuid().ToString("N");

        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(Ruta, Solicitud(correlacion, nivel: nivel));

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var evento = Registrado(correlacion);
        Assert.Equal(esperado, evento.Level);
        Assert.Equal("\"frontend\"", evento.Properties["Origen"].ToString());
        Assert.Equal("\"/productos/5\"", evento.Properties["RutaCliente"].ToString());
        Assert.Equal("\"TypeError\"", evento.Properties["TipoCliente"].ToString());
    }

    [Fact]
    public async Task Registrar_SinCorrelacionNiTipo_UsaLaDeLaPeticion()
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(
            Ruta, new { nivel = "Error", mensaje = "Sin correlación", ruta = "/" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Registrado(Assert.Single(respuesta.Headers.GetValues("X-Correlacion")));
    }

    [Theory]
    [InlineData("Authorization: Bearer abc.def.ghi", "abc.def.ghi")]
    [InlineData("eyJhbGciOiJI.eyJzdWIiOiIx.firma", "eyJ")]
    [InlineData("{\"contrasena\":\"Mi clave 1\"}", "Mi clave 1")]
    [InlineData("contraseña=Secreta1", "Secreta1")]
    [InlineData("password: Secreta1", "Secreta1")]
    [InlineData("hash $2a$12$abcdefghijklmnopqrstuv", "abcdefghij")]
    [InlineData("usuario ana.perez@ejemplo.com falló", "ana.perez")]
    [InlineData("GET /api/x?codigo=personal falló", "personal")]
    [InlineData("documento 0912345678", "0912345678")]
    [InlineData("valor 0123456789abcdef0123456789abcdef0123", "abcdef")]
    [InlineData("uno\n[ERR] línea falsa", "\n")]
    public async Task Registrar_OcultaDatosSensibles(string mensaje, string prohibido)
    {
        var correlacion = Guid.NewGuid().ToString("N");

        await fabrica.CreateClient().PostAsJsonAsync(Ruta, Solicitud(correlacion, mensaje));

        Assert.DoesNotContain(prohibido, Registrado(correlacion).RenderMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{}", "nivel")]
    [InlineData("""{ "nivel": 9, "mensaje": "a", "ruta": "/" }""", "nivel")]
    [InlineData("""{ "nivel": "Otro", "mensaje": "a", "ruta": "/" }""", "nivel")]
    [InlineData("""{ "nivel": "Error", "mensaje": " ", "ruta": "/" }""", "mensaje")]
    [InlineData("""{ "nivel": "Error", "mensaje": "a", "ruta": "productos" }""", "ruta")]
    [InlineData("""{ "nivel": "Error", "mensaje": "a", "ruta": "/a?token=1" }""", "ruta")]
    [InlineData("""{ "nivel": "Error", "mensaje": "a", "ruta": "/", "tipo": "no válido" }""", "tipo")]
    [InlineData("""{ "nivel": "Error", "mensaje": "a", "ruta": "/", "correlacion": "a b" }""", "correlacion")]
    public async Task Registrar_ConDatosInvalidos_Devuelve400(string cuerpo, string campo)
    {
        var contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");

        var respuesta = await fabrica.CreateClient().PostAsync(Ruta, contenido);

        Assert.NotEmpty((await respuesta.ProblemaAsync(HttpStatusCode.BadRequest)).Errores(campo));
    }

    [Theory]
    [InlineData(501, 1, "mensaje")]
    [InlineData(1, 201, "ruta")]
    public async Task Registrar_ConTextosLargos_Devuelve400(int mensaje, int ruta, string campo)
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(
            Ruta, new { nivel = "Error", mensaje = new string('a', mensaje), ruta = "/" + new string('a', ruta - 1) });

        Assert.NotEmpty((await respuesta.ProblemaAsync(HttpStatusCode.BadRequest)).Errores(campo));
    }

    [Fact]
    public async Task Registrar_ConCuerpoMayorAlLimite_Devuelve413()
    {
        await using var conKestrel = fabrica.WithWebHostBuilder(_ => { });
        conKestrel.UseKestrel(0);
        using var cliente = conKestrel.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(Ruta, Solicitud("a", new string('a', 5000)));

        await respuesta.ProblemaAsync(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Registrar_SuperandoElLimite_Devuelve429SinAfectarElLogin()
    {
        await using var limitada = fabrica.WithWebHostBuilder(constructor =>
            constructor.UseSetting("LimiteRegistrosCliente:PeticionesPorMinuto", "2"));
        using var cliente = limitada.CreateClient();

        for (var intento = 0; intento < 2; intento++)
            await cliente.PostAsJsonAsync(Ruta, Solicitud("a"));
        var respuesta = await cliente.PostAsJsonAsync(Ruta, Solicitud("a"));
        var login = await cliente.PostAsJsonAsync("/api/autenticacion/login", new { });

        await respuesta.ProblemaAsync(HttpStatusCode.TooManyRequests);
        Assert.NotNull(respuesta.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task Swagger_DocumentaElEndpointPublico()
    {
        var documento = JsonDocument.Parse(await fabrica.CreateClient().GetStringAsync("/swagger/v1/swagger.json")).RootElement;
        var operacion = documento.GetProperty("paths").GetProperty(Ruta).GetProperty("post");

        Assert.False(operacion.TryGetProperty("security", out _));
        Assert.Equal(["204", "400", "413", "415", "429"],
            operacion.GetProperty("responses").EnumerateObject().Select(respuesta => respuesta.Name).Order());
        Assert.True(documento.GetProperty("components").GetProperty("schemas")
            .GetProperty(nameof(SolicitudRegistroCliente)).TryGetProperty("examples", out _));
    }
}
