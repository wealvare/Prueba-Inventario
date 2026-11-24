using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Errores;

internal sealed class ResultadoProblema(ProblemDetails problema) : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context) => Problemas.EscribirAsync(context.HttpContext, problema);
}
