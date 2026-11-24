using System.Security.Claims;

namespace Inventario.Api.Autorizacion;

internal static class IdentidadUsuario
{
    public static int? LeerId(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirstValue("sub"), out var id) ? id : null;
}
