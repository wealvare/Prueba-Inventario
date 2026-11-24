using Microsoft.AspNetCore.Authorization;

namespace Inventario.Api.Autorizacion;

internal sealed class RequisitoAdministradorActivo : IAuthorizationRequirement
{
    public const string Politica = "AdministradorActivo";
}
