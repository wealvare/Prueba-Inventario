using Inventario.Aplicacion.RegistrosCliente.Dtos;
using Inventario.Aplicacion.RegistrosCliente.Puertos.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventario.Api.RegistrosCliente;

[ApiController]
[Route("api/registros-cliente")]
[Consumes("application/json")]
[Produces("application/json")]
[SwaggerTag("Errores del frontend")]
public sealed class RegistrosClienteController(IRegistrarErrorCliente registrarErrorCliente) : ControllerBase
{
    private const int TamanoMaximo = 4096;

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RegistroApi.PoliticaRegistrosCliente)]
    [RequestSizeLimit(TamanoMaximo)]
    [SwaggerOperation(
        "Registra un error del frontend",
        "Público, con límite por IP y hasta 4 KB. Oculta tokens, contraseñas, correos y números largos.")]
    [SwaggerResponse(204, "Error registrado.")]
    [SwaggerResponse(400, "Los datos no son válidos.", typeof(ValidationProblemDetails), "application/problem+json")]
    [SwaggerResponse(413, "El cuerpo supera los 4 KB.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(415, "El contenido no es JSON.", typeof(ProblemDetails), "application/problem+json")]
    [SwaggerResponse(429, "Se superó el límite de registros.", typeof(ProblemDetails), "application/problem+json")]
    public async Task<IActionResult> Registrar(SolicitudRegistroCliente solicitud, CancellationToken cancelacion)
    {
        await registrarErrorCliente.EjecutarAsync(solicitud, cancelacion);
        return NoContent();
    }
}
