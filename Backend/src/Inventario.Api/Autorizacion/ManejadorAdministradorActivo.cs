using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Dominio.Usuarios;
using Microsoft.AspNetCore.Authorization;

namespace Inventario.Api.Autorizacion;

internal sealed class ManejadorAdministradorActivo(
    IVerificarAdministrador verificarAdministrador,
    IHttpContextAccessor acceso) : AuthorizationHandler<RequisitoAdministradorActivo>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequisitoAdministradorActivo requirement)
    {
        if (!context.User.IsInRole(nameof(RolUsuario.Administrador)) || context.User.LeerId() is not { } id)
            return;

        if (await verificarAdministrador.EjecutarAsync(id, acceso.HttpContext?.RequestAborted ?? CancellationToken.None))
            context.Succeed(requirement);
    }
}
