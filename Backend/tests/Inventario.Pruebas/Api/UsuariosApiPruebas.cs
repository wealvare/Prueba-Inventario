using System.Net;
using System.Net.Http.Json;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Dominio.Usuarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inventario.Pruebas.Api;

[Collection("Api")]
[Trait("Categoria", "Integracion")]
public sealed class UsuariosApiPruebas(FabricaApi fabrica)
{
    private const string Ruta = "/api/usuarios";
    private const string RutaRegistro = "/api/autenticacion/registro";
    private const string RutaLogin = "/api/autenticacion/login";
    private const string Contrasena = "Clave123";

    private readonly HttpClient _anonimo = fabrica.CreateClient();
    private readonly HttpClient _administrador = fabrica.ClienteCon(RolUsuario.Administrador);
    private readonly HttpClient _consulta = fabrica.ClienteCon(RolUsuario.Consulta);

    private int IdAdministrador => int.Parse(new JsonWebToken(fabrica.Token(RolUsuario.Administrador)).Subject);

    private static string Unico() => $"u{Guid.NewGuid():N}"[..12];

    private static object Registro(string nombre, string? correo = null, string contrasena = Contrasena) =>
        new { nombreUsuario = nombre, correo = correo ?? $"{nombre}@inventario.local", contrasena };

    private static object Nuevo(string nombre, string? rol, string? correo = null) =>
        new { nombreUsuario = nombre, correo = correo ?? $"{nombre}@inventario.local", contrasena = Contrasena, rol };

    private static object Cambio(
        UsuarioDto usuario,
        string? rol = null,
        bool? activo = null,
        string? contrasena = null,
        string? nombre = null) => new
        {
            nombreUsuario = nombre ?? usuario.NombreUsuario,
            correo = usuario.Correo,
            rol = rol ?? usuario.Rol,
            activo = activo ?? usuario.Activo,
            contrasena
        };

    private async Task<UsuarioDto> CrearAsync(string rol = "Consulta")
    {
        var respuesta = await _administrador.PostAsJsonAsync(Ruta, Nuevo(Unico(), rol));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<UsuarioDto>())!;
    }

    private Task<HttpResponseMessage> EntrarAsync(string nombreUsuario, string contrasena = Contrasena) =>
        _anonimo.PostAsJsonAsync(RutaLogin, new { nombreUsuario, contrasena });

    private async Task<RespuestaInicioSesion> SesionAsync(string nombreUsuario)
    {
        var respuesta = await EntrarAsync(nombreUsuario);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesion>())!;
    }

    private async Task<UsuarioDto> LeerAsync(int id) =>
        (await _administrador.GetFromJsonAsync<UsuarioDto>($"{Ruta}/{id}"))!;

    [Fact]
    public async Task Registro_CreaUnaCuentaDeConsultaQuePuedeEntrar()
    {
        var nombre = Unico();

        var respuesta = await _anonimo.PostAsJsonAsync(RutaRegistro, Registro(nombre));

        var contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.DoesNotContain("hash", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrasena", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Consulta", (await SesionAsync(nombre)).Rol);
    }

    [Fact]
    public async Task Registro_IgnoraElRolEnviado()
    {
        var nombre = Unico();

        var respuesta = await _anonimo.PostAsJsonAsync(RutaRegistro, Nuevo(nombre, "Administrador"));

        var creado = (await respuesta.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal(("Consulta", true), (creado.Rol, creado.Activo));
        Assert.Equal("Consulta", (await LeerAsync(creado.Id)).Rol);
        Assert.Equal("Consulta", (await SesionAsync(nombre)).Rol);
    }

    [Fact]
    public async Task Registro_ConDatosInvalidos_Devuelve400PorCampo()
    {
        var respuesta = await _anonimo.PostAsJsonAsync(RutaRegistro, Registro("a b", "correo", "clave"));

        var problema = await respuesta.ProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Single(problema.Errores("nombreUsuario"));
        Assert.Equal(["El correo no tiene un formato válido."], problema.Errores("correo"));
        Assert.Equal(
            ["La contraseña debe tener entre 8 y 100 caracteres, con mayúscula, minúscula y número."],
            problema.Errores("contrasena"));
    }

    [Fact]
    public async Task Registro_Duplicado_Devuelve409()
    {
        var existente = await CrearAsync();
        var mayusculas = existente.NombreUsuario.ToUpperInvariant();

        var nombre = await _anonimo.PostAsJsonAsync(RutaRegistro, Registro(mayusculas, $"{Unico()}@inventario.local"));
        var correo = await _anonimo.PostAsJsonAsync(RutaRegistro, Registro(Unico(), existente.Correo));

        Assert.Equal(
            $"Ya existe un usuario con el nombre '{mayusculas}'.",
            (await nombre.ProblemaAsync(HttpStatusCode.Conflict)).Detalle());
        Assert.Equal(
            $"Ya existe un usuario con el correo '{existente.Correo}'.",
            (await correo.ProblemaAsync(HttpStatusCode.Conflict)).Detalle());
    }

    [Fact]
    public async Task Registro_EnParalelo_SoloCreaUno()
    {
        var solicitud = Registro(Unico());

        var respuestas = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => _anonimo.PostAsJsonAsync(RutaRegistro, solicitud)));

        Assert.Single(respuestas, respuesta => respuesta.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Registro_ComparteElLimiteDelLogin()
    {
        await using var limitada = fabrica.WithWebHostBuilder(constructor =>
            constructor.UseSetting("LimiteInicioSesion:PeticionesPorMinuto", "2"));
        using var cliente = limitada.CreateClient();

        await (await cliente.PostAsJsonAsync(RutaLogin, new { })).ProblemaAsync(HttpStatusCode.BadRequest);
        await (await cliente.PostAsJsonAsync(RutaRegistro, new { })).ProblemaAsync(HttpStatusCode.BadRequest);
        var respuesta = await cliente.PostAsJsonAsync(RutaRegistro, new { });

        await respuesta.ProblemaAsync(HttpStatusCode.TooManyRequests);
        Assert.NotNull(respuesta.Headers.RetryAfter);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/1")]
    [InlineData("POST", "")]
    [InlineData("PUT", "/1")]
    [InlineData("DELETE", "/1")]
    public async Task Administracion_ExigeSesionDeAdministrador(string metodo, string sufijo)
    {
        HttpRequestMessage Solicitud() => new(new HttpMethod(metodo), Ruta + sufijo)
        {
            Content = metodo is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        };

        await (await _anonimo.SendAsync(Solicitud())).ProblemaAsync(HttpStatusCode.Unauthorized);
        var problema = await (await _consulta.SendAsync(Solicitud())).ProblemaAsync(HttpStatusCode.Forbidden);
        Assert.Equal("No tienes permiso para realizar esta acción.", problema.Detalle());
    }

    [Fact]
    public async Task Crud_Completo()
    {
        var creacion = await _administrador.PostAsJsonAsync(Ruta, Nuevo(Unico(), "Administrador"));
        var creado = (await creacion.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        Assert.Equal($"{Ruta}/{creado.Id}", creacion.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.Equal("Administrador", creado.Rol);

        var nombre = Unico();
        var edicion = await _administrador.PutAsJsonAsync(
            $"{Ruta}/{creado.Id}", Cambio(creado, "Consulta", contrasena: "Nueva456", nombre: nombre));
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        var editado = await LeerAsync(creado.Id);
        Assert.Equal((nombre, "Consulta", true), (editado.NombreUsuario, editado.Rol, editado.Activo));
        Assert.Equal(HttpStatusCode.Unauthorized, (await EntrarAsync(nombre)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(nombre, "Nueva456")).StatusCode);

        var desactivacion = await _administrador.DeleteAsync($"{Ruta}/{creado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivacion.StatusCode);
        Assert.False((await LeerAsync(creado.Id)).Activo);
    }

    [Fact]
    public async Task Editar_SinContrasena_ConservaLaActual()
    {
        var usuario = await CrearAsync();

        var respuesta = await _administrador.PutAsJsonAsync($"{Ruta}/{usuario.Id}", Cambio(usuario));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(usuario.NombreUsuario)).StatusCode);
    }

    [Fact]
    public async Task Listar_PaginaYNuncaDevuelveElHash()
    {
        await CrearAsync();

        var pagina = (await _administrador.GetFromJsonAsync<ResultadoPaginado<UsuarioDto>>(
            $"{Ruta}?pagina=2&tamanoPagina=1"))!;
        var texto = await _administrador.GetStringAsync(Ruta);

        Assert.Equal((2, 1, 1), (pagina.Pagina, pagina.TamanoPagina, pagina.Elementos.Count));
        Assert.True(pagina.TotalElementos >= 3);
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$2", texto);
        await (await _administrador.GetAsync($"{Ruta}?pagina=0")).ProblemaAsync(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Inexistente")]
    [InlineData("99")]
    [InlineData(null)]
    public async Task Crear_ConRolInvalido_Devuelve400(string? rol)
    {
        var respuesta = await _administrador.PostAsJsonAsync(Ruta, Nuevo(Unico(), rol));

        Assert.Single((await respuesta.ProblemaAsync(HttpStatusCode.BadRequest)).Errores("rol"));
    }

    [Fact]
    public async Task Duplicados_AlCrearYEditar_Devuelven409()
    {
        var existente = await CrearAsync();
        var otro = await CrearAsync();

        var creacion = await _administrador.PostAsJsonAsync(Ruta, Nuevo(Unico(), "Consulta", existente.Correo));
        var edicion = await _administrador.PutAsJsonAsync(
            $"{Ruta}/{otro.Id}", Cambio(otro, nombre: existente.NombreUsuario));

        await creacion.ProblemaAsync(HttpStatusCode.Conflict);
        await edicion.ProblemaAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UsuarioInexistente_Devuelve404()
    {
        var ruta = $"{Ruta}/{int.MaxValue}";
        var cambio = Cambio(new UsuarioDto(0, Unico(), $"{Unico()}@inventario.local", "Consulta", true));

        await (await _administrador.GetAsync(ruta)).ProblemaAsync(HttpStatusCode.NotFound);
        await (await _administrador.PutAsJsonAsync(ruta, cambio)).ProblemaAsync(HttpStatusCode.NotFound);
        var problema = await (await _administrador.DeleteAsync(ruta)).ProblemaAsync(HttpStatusCode.NotFound);

        Assert.Equal($"No existe un usuario con el id {int.MaxValue}.", problema.Detalle());
    }

    [Fact]
    public async Task CuentaPropia_NoPuedePerderElRolNiDesactivarse()
    {
        var propia = await LeerAsync(IdAdministrador);
        var ruta = $"{Ruta}/{propia.Id}";

        HttpResponseMessage[] respuestas =
        [
            await _administrador.PutAsJsonAsync(ruta, Cambio(propia, rol: "Consulta")),
            await _administrador.PutAsJsonAsync(ruta, Cambio(propia, activo: false)),
            await _administrador.DeleteAsync(ruta)
        ];

        foreach (var respuesta in respuestas)
            Assert.Equal(
                "No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.",
                (await respuesta.ProblemaAsync(HttpStatusCode.Conflict)).Detalle());
        Assert.Equal(propia, await LeerAsync(propia.Id));
    }

    [Fact]
    public async Task UsuarioInactivo_RecibeElMismoErrorQueConCredencialesIncorrectas()
    {
        var usuario = await CrearAsync();
        await _administrador.DeleteAsync($"{Ruta}/{usuario.Id}");

        var problema = await (await EntrarAsync(usuario.NombreUsuario)).ProblemaAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("El usuario o la contraseña no son correctos.", problema.Detalle());

        await _administrador.PutAsJsonAsync($"{Ruta}/{usuario.Id}", Cambio(usuario, activo: true));
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(usuario.NombreUsuario)).StatusCode);
    }

    [Theory]
    [InlineData("Administrador", false)]
    [InlineData("Consulta", true)]
    public async Task AdministradorSinPermisoVigente_Recibe403AunqueSuTokenNoExpire(string rol, bool activo)
    {
        var otro = await CrearAsync("Administrador");
        var cliente = fabrica.ClienteCon((await SesionAsync(otro.NombreUsuario)).Token);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Ruta)).StatusCode);

        await _administrador.PutAsJsonAsync($"{Ruta}/{otro.Id}", Cambio(otro, rol, activo));

        await (await cliente.GetAsync(Ruta)).ProblemaAsync(HttpStatusCode.Forbidden);
        await (await cliente.PutAsJsonAsync($"{Ruta}/{otro.Id}", Cambio(otro))).ProblemaAsync(HttpStatusCode.Forbidden);
        await (await cliente.DeleteAsync($"{Ruta}/{IdAdministrador}")).ProblemaAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Swagger_DocumentaLosEndpoints()
    {
        var documento = await _anonimo.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains(RutaRegistro, documento);
        Assert.Contains($"{Ruta}/{{id}}", documento);
        Assert.DoesNotContain("hashContrasena", documento);
    }
}
