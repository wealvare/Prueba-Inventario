using Inventario.Api.Autorizacion;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventario.Api.Usuarios;

[ApiController]
[Authorize(Policy = RequisitoAdministradorActivo.Politica)]
[Route("api/usuarios")]
[Produces("application/json")]
[SwaggerTag("Administración de usuarios, solo para administradores activos")]
[SwaggerResponse(401, "Falta el token o no es válido.", typeof(ProblemDetails), "application/problem+json")]
[SwaggerResponse(403, "El usuario no es un administrador activo.", typeof(ProblemDetails), "application/problem+json")]
public sealed class UsuariosController(
    IListarUsuarios listarUsuarios,
    IObtenerUsuario obtenerUsuario,
    ICrearUsuario crearUsuario,
    IActualizarUsuario actualizarUsuario,
    IDesactivarUsuario desactivarUsuario) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("Lista los usuarios", "Incluye activos e inactivos, ordenados por nombre de usuario.")]
    [SwaggerResponse(200, "Página de usuarios.", typeof(ResultadoPaginado<UsuarioDto>))]
    [SwaggerResponse(400, "La paginación no es válida.", typeof(ValidationProblemDetails), "application/problem+json")]
    public async Task<ActionResult<ResultadoPaginado<UsuarioDto>>> Listar(
        [FromQuery] FiltroUsuarios filtro,
        CancellationToken cancelacion) =>
        Ok(await listarUsuarios.EjecutarAsync(filtro, cancelacion));

    [HttpGet("{id:int}")]
    [SwaggerOperation("Obtiene un usuario", "Nunca incluye la contraseña ni su hash.")]
    [SwaggerResponse(200, "Usuario encontrado.", typeof(UsuarioDto))]
    [SwaggerResponse(404, "El usuario no existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await obtenerUsuario.EjecutarAsync(id, cancelacion));

    [HttpPost]
    [SwaggerOperation("Crea un usuario", "El usuario queda activo con el rol indicado.")]
    [SwaggerResponse(201, "Usuario creado. El encabezado Location contiene su dirección.", typeof(UsuarioDto))]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "El nombre de usuario o el correo ya existe.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Crear(SolicitudCrearUsuario solicitud, CancellationToken cancelacion)
    {
        var usuario = await crearUsuario.EjecutarAsync(solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        "Actualiza un usuario",
        "Sin contrasena se conserva la actual. Nadie puede quitarse el rol ni desactivarse, y debe quedar un administrador activo.")]
    [SwaggerResponse(200, "Usuario actualizado.", typeof(UsuarioDto))]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(404, "El usuario no existe.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "Datos duplicados, cuenta propia o último administrador activo.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Actualizar(
        int id,
        SolicitudActualizarUsuario solicitud,
        CancellationToken cancelacion) =>
        Ok(await actualizarUsuario.EjecutarAsync(id, solicitud, IdActor, cancelacion));

    [HttpDelete("{id:int}")]
    [SwaggerOperation("Desactiva un usuario", "El usuario se conserva, pero ya no puede iniciar sesión.")]
    [SwaggerResponse(204, "Usuario desactivado.")]
    [SwaggerResponse(404, "El usuario no existe.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(409, "Cuenta propia o último administrador activo.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancelacion)
    {
        await desactivarUsuario.EjecutarAsync(id, IdActor, cancelacion);
        return NoContent();
    }

    private int IdActor => User.LeerId()!.Value;
}
