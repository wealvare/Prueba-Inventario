using System.Text;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Seguridad;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Inventario.Pruebas.Infraestructura;

public sealed class ServicioTokenPruebas
{
    private static readonly OpcionesJwt Opciones = new(new string('k', 32), "emisor", "audiencia", 30);

    [Theory]
    [InlineData(RolUsuario.Administrador)]
    [InlineData(RolUsuario.Consulta)]
    public async Task Generar_FirmaElTokenConRolYExpiracion(RolUsuario rol)
    {
        var usuario = Usuario.Reconstruir(5, "usuario", "usuario@inventario.local", "hash", rol);

        var token = new ServicioToken(Opciones, TimeProvider.System).Generar(usuario);

        var resultado = await ValidarAsync(token.Valor, Opciones.Clave);
        var leido = Assert.IsType<JsonWebToken>(resultado.SecurityToken);
        Assert.True(resultado.IsValid);
        Assert.Equal("5", leido.Subject);
        Assert.Equal(rol.ToString(), leido.GetClaim("role").Value);
        Assert.Equal(token.Expiracion, leido.ValidTo, TimeSpan.FromSeconds(1));
        Assert.Equal(30, (leido.ValidTo - leido.ValidFrom).TotalMinutes);
    }

    [Fact]
    public async Task Generar_NoEsValidoConOtraClave()
    {
        var usuario = new Usuario("usuario", "usuario@inventario.local", "hash", RolUsuario.Consulta);

        var token = new ServicioToken(Opciones, TimeProvider.System).Generar(usuario);

        Assert.False((await ValidarAsync(token.Valor, new string('x', 32))).IsValid);
    }

    private static Task<TokenValidationResult> ValidarAsync(string token, string clave) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Opciones.Emisor,
            ValidAudience = Opciones.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave))
        });
}
