using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Dominio.Usuarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Testcontainers.MsSql;

namespace Inventario.Pruebas.Api;

public sealed class FabricaApi : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ClaveJwt = "clave-de-pruebas-con-mas-de-32-bytes";
    public const string Emisor = "emisor-pruebas";
    public const string Audiencia = "audiencia-pruebas";
    public const string OrigenPermitido = "http://localhost:4200";
    public const string ContrasenaAdministrador = "contrasena-administrador";
    public const string ContrasenaConsulta = "contrasena-consulta";

    private readonly MsSqlContainer _contenedor =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly Dictionary<RolUsuario, string> _tokens = [];

    public SumideroRegistros Registros { get; } = new();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        using var cliente = CreateClient();
        _tokens[RolUsuario.Administrador] = await IniciarSesionAsync(cliente, "admin", ContrasenaAdministrador);
        _tokens[RolUsuario.Consulta] = await IniciarSesionAsync(cliente, "consulta", ContrasenaConsulta);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _contenedor.DisposeAsync();
    }

    public string Token(RolUsuario rol) => _tokens[rol];

    public HttpClient ClienteCon(RolUsuario rol) => ClienteCon(Token(rol));

    public HttpClient ClienteCon(string token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var conexion = new SqlConnectionStringBuilder(_contenedor.GetConnectionString());
        var servidor = conexion.DataSource.Split(',');

        builder.UseEnvironment("Development");
        builder.UseSetting("BaseDatos:Servidor", servidor[0]);
        builder.UseSetting("BaseDatos:Puerto", servidor[1]);
        builder.UseSetting("BaseDatos:Nombre", "InventarioApi");
        builder.UseSetting("BaseDatos:Usuario", conexion.UserID);
        builder.UseSetting("BaseDatos:Contrasena", conexion.Password);
        builder.UseSetting("BaseDatos:ConfiarCertificado", "true");
        builder.UseSetting("Jwt:Clave", ClaveJwt);
        builder.UseSetting("Jwt:Emisor", Emisor);
        builder.UseSetting("Jwt:Audiencia", Audiencia);
        builder.UseSetting("Jwt:MinutosExpiracion", "30");
        builder.UseSetting("UsuariosIniciales:Administrador:NombreUsuario", "admin");
        builder.UseSetting("UsuariosIniciales:Administrador:Correo", "admin@inventario.local");
        builder.UseSetting("UsuariosIniciales:Administrador:Contrasena", ContrasenaAdministrador);
        builder.UseSetting("UsuariosIniciales:Consulta:NombreUsuario", "consulta");
        builder.UseSetting("UsuariosIniciales:Consulta:Correo", "consulta@inventario.local");
        builder.UseSetting("UsuariosIniciales:Consulta:Contrasena", ContrasenaConsulta);
        builder.UseSetting("Cors:OrigenesPermitidos:0", OrigenPermitido);
        builder.UseSetting("LimiteInicioSesion:PeticionesPorMinuto", "1000");
        builder.ConfigureServices(servicios => servicios.AddSingleton<ILogEventSink>(Registros));
    }

    private static async Task<string> IniciarSesionAsync(HttpClient cliente, string nombreUsuario, string contrasena)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/login", new { nombreUsuario, contrasena });
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesion>())!.Token;
    }
}

[CollectionDefinition("Api")]
public sealed class ColeccionApi : ICollectionFixture<FabricaApi>;
