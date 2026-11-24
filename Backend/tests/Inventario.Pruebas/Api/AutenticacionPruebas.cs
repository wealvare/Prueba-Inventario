using System.Net;
using System.Net.Http.Json;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Hosting;

namespace Inventario.Pruebas.Api;

[Collection("Api")]
[Trait("Categoria", "Integracion")]
public sealed class AutenticacionPruebas(FabricaApi fabrica)
{
    private const string Ruta = "/api/autenticacion/login";

    [Theory]
    [InlineData("admin", FabricaApi.ContrasenaAdministrador, "Administrador")]
    [InlineData("consulta", FabricaApi.ContrasenaConsulta, "Consulta")]
    public async Task Login_Correcto_DevuelveToken(string nombreUsuario, string contrasena, string rol)
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(Ruta, new { nombreUsuario, contrasena });

        var sesion = await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesion>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal((nombreUsuario, rol), (sesion!.NombreUsuario, sesion.Rol));
        Assert.True(sesion.Expiracion > DateTime.UtcNow);
        Assert.NotEmpty(sesion.Token);
    }

    [Theory]
    [InlineData("admin", "incorrecta")]
    [InlineData("desconocido", FabricaApi.ContrasenaAdministrador)]
    public async Task Login_Incorrecto_Devuelve401(string nombreUsuario, string contrasena)
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(Ruta, new { nombreUsuario, contrasena });

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("El usuario o la contraseña no son correctos.", problema.Detalle());
    }

    [Fact]
    public async Task Login_SinDatos_Devuelve400()
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(Ruta, new { });

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["El nombre de usuario es obligatorio."], problema.Errores("nombreUsuario"));
        Assert.Equal(["La contraseña es obligatoria."], problema.Errores("contrasena"));
    }

    [Fact]
    public async Task Login_SuperandoElLimite_Devuelve429()
    {
        await using var limitada = fabrica.WithWebHostBuilder(constructor =>
            constructor.UseSetting("LimiteInicioSesion:PeticionesPorMinuto", "2"));
        using var cliente = limitada.CreateClient();

        for (var intento = 0; intento < 2; intento++)
            await (await cliente.PostAsJsonAsync(Ruta, new { })).ProblemaAsync(HttpStatusCode.BadRequest);
        var respuesta = await cliente.PostAsJsonAsync(Ruta, new { });

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal("Espera un momento antes de volver a intentarlo.", problema.Detalle());
        Assert.NotNull(respuesta.Headers.RetryAfter);
    }

    [Theory]
    [InlineData("GET", "/api/productos")]
    [InlineData("GET", "/api/productos/1")]
    [InlineData("POST", "/api/productos")]
    [InlineData("PUT", "/api/productos/1")]
    [InlineData("DELETE", "/api/productos/1")]
    public async Task SinToken_Devuelve401(string metodo, string ruta)
    {
        var respuesta = await fabrica.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta));

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("Debes iniciar sesión con un token válido.", problema.Detalle());
        Assert.NotEmpty(respuesta.Headers.WwwAuthenticate);
    }

    [Theory]
    [InlineData("otra-clave-de-pruebas-con-mas-de-32-bytes", FabricaApi.Emisor, FabricaApi.Audiencia, 0)]
    [InlineData(FabricaApi.ClaveJwt, "otro", FabricaApi.Audiencia, 0)]
    [InlineData(FabricaApi.ClaveJwt, FabricaApi.Emisor, "otra", 0)]
    [InlineData(FabricaApi.ClaveJwt, FabricaApi.Emisor, FabricaApi.Audiencia, -32)]
    public async Task ConTokenInvalido_Devuelve401(string clave, string emisor, string audiencia, int minutos)
    {
        var reloj = new RelojDesplazado(TimeSpan.FromMinutes(minutos));
        var usuario = Usuario.Reconstruir(1, "admin", "admin@inventario.local", "hash", RolUsuario.Administrador);
        var token = new ServicioToken(new OpcionesJwt(clave, emisor, audiencia, 30), reloj).Generar(usuario);

        var respuesta = await fabrica.ClienteCon(token.Valor).GetAsync("/api/productos");

        await respuesta.ProblemaAsync(HttpStatusCode.Unauthorized);
    }

    private sealed class RelojDesplazado(TimeSpan diferencia) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + diferencia;
    }
}
