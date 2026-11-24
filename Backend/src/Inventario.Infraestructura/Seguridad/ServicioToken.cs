using System.Security.Claims;
using System.Text;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Inventario.Infraestructura.Seguridad;

public sealed class ServicioToken(OpcionesJwt opciones, TimeProvider reloj) : IServicioToken
{
    public TokenAcceso Generar(Usuario usuario)
    {
        var emision = reloj.GetUtcNow().UtcDateTime;
        var expiracion = emision.AddMinutes(opciones.MinutosExpiracion);
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Clave));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opciones.Emisor,
            Audience = opciones.Audiencia,
            IssuedAt = emision,
            NotBefore = emision,
            Expires = expiracion,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
                new Claim("role", usuario.Rol.ToString())
            ])
        };

        return new TokenAcceso(new JsonWebTokenHandler().CreateToken(descriptor), expiracion);
    }
}
