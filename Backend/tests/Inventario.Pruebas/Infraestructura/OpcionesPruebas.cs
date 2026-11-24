using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Inventario.Pruebas.Infraestructura;

public sealed class OpcionesPruebas
{
    private readonly Dictionary<string, string?> _valores = new()
    {
        ["BaseDatos:Servidor"] = "localhost",
        ["BaseDatos:Puerto"] = "1433",
        ["BaseDatos:Nombre"] = "Inventario",
        ["BaseDatos:Usuario"] = "sa",
        ["BaseDatos:Contrasena"] = "a;b=c",
        ["Jwt:Clave"] = new string('k', 32),
        ["Jwt:Emisor"] = "emisor",
        ["Jwt:Audiencia"] = "audiencia",
        ["Jwt:MinutosExpiracion"] = "60",
        ["UsuariosIniciales:Administrador:NombreUsuario"] = "admin",
        ["UsuariosIniciales:Administrador:Correo"] = "admin@inventario.local",
        ["UsuariosIniciales:Administrador:Contrasena"] = "contrasena-a",
        ["UsuariosIniciales:Consulta:NombreUsuario"] = "consulta",
        ["UsuariosIniciales:Consulta:Correo"] = "consulta@inventario.local",
        ["UsuariosIniciales:Consulta:Contrasena"] = "contrasena-c"
    };

    private IConfiguration Configuracion => new ConfigurationBuilder().AddInMemoryCollection(_valores).Build();

    [Fact]
    public void BaseDatos_ConservaCaracteresEspecialesDeLaContrasena()
    {
        var cadena = new SqlConnectionStringBuilder(OpcionesBaseDatos.Leer(Configuracion).CadenaConexion);

        Assert.Equal("a;b=c", cadena.Password);
        Assert.Equal("localhost,1433", cadena.DataSource);
        Assert.False(cadena.TrustServerCertificate);
    }

    [Fact]
    public void Jwt_LeeLosValores() =>
        Assert.Equal(new OpcionesJwt(new string('k', 32), "emisor", "audiencia", 60), OpcionesJwt.Leer(Configuracion));

    [Theory]
    [InlineData("Jwt:Clave", "")]
    [InlineData("Jwt:Clave", "corta")]
    [InlineData("Jwt:Emisor", null)]
    [InlineData("Jwt:MinutosExpiracion", "0")]
    [InlineData("Jwt:MinutosExpiracion", "texto")]
    public void Jwt_ConValoresInvalidos_Falla(string clave, string? valor)
    {
        _valores[clave] = valor;

        Assert.Throws<ExcepcionConfiguracion>(() => OpcionesJwt.Leer(Configuracion));
    }

    [Fact]
    public void UsuariosIniciales_LeeUnUsuarioPorRol() =>
        Assert.Equal(
            [RolUsuario.Administrador, RolUsuario.Consulta],
            OpcionesUsuariosIniciales.Leer(Configuracion).Usuarios.Select(usuario => usuario.Rol));

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    public void UsuariosIniciales_ConContrasenaInvalida_Falla(string contrasena)
    {
        _valores["UsuariosIniciales:Consulta:Contrasena"] = contrasena;

        Assert.Throws<ExcepcionConfiguracion>(() => OpcionesUsuariosIniciales.Leer(Configuracion));
    }

    [Fact]
    public void UsuariosIniciales_SinVariables_FallaNombrandoTodasLasQueFaltan()
    {
        _valores.Remove("UsuariosIniciales:Administrador:Correo");
        _valores.Remove("UsuariosIniciales:Consulta:NombreUsuario");

        var error = Assert.Throws<ExcepcionConfiguracion>(() => OpcionesUsuariosIniciales.Leer(Configuracion));

        Assert.Contains(
            "UsuariosIniciales__Administrador__Correo, UsuariosIniciales__Consulta__NombreUsuario", error.Message);
        Assert.Contains(".env.example", error.Message);
    }
}
