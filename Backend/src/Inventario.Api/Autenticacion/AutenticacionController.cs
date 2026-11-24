using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventario.Api.Autenticacion;

[ApiController]
[Route("api/autenticacion")]
[Produces("application/json")]
[SwaggerTag("Inicio de sesión y registro")]
public sealed class AutenticacionController(
    IIniciarSesion iniciarSesion,
    IRegistrarUsuario registrarUsuario) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RegistroApi.PoliticaInicioSesion)]
    [SwaggerOperation("Inicia sesión", "Devuelve un token JWT para usar en los demás endpoints.")]
    [SwaggerResponse(200, "Sesión iniciada.", typeof(RespuestaInicioSesion))]
    [SwaggerResponse(400, "Faltan datos o no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(401, "El usuario o la contraseña no son correctos.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(429, "Se superó el límite de intentos.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<RespuestaInicioSesion>> IniciarSesion(
        SolicitudInicioSesion solicitud,
        CancellationToken cancelacion) =>
        Ok(await iniciarSesion.EjecutarAsync(solicitud, cancelacion));

    [HttpPost("registro")]
    [AllowAnonymous]
    [EnableRateLimiting(RegistroApi.PoliticaInicioSesion)]
    [SwaggerOperation("Crea una cuenta", "Registro público. La cuenta siempre tiene el rol Consulta y comparte el límite del login.")]
    [SwaggerResponse(201, "Cuenta creada.", typeof(UsuarioDto))]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "El nombre de usuario o el correo ya existe.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(429, "Se superó el límite de intentos.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Registrar(SolicitudRegistro solicitud, CancellationToken cancelacion) =>
        StatusCode(StatusCodes.Status201Created, await registrarUsuario.EjecutarAsync(solicitud, cancelacion));
}
