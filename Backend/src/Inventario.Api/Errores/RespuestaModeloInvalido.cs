using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Errores;

internal static class RespuestaModeloInvalido
{
    private const string PrefijoJson = "$";

    public static IActionResult Crear(ActionContext contexto)
    {
        var entradas = contexto.ModelState.Where(entrada => entrada.Value!.Errors.Count > 0).ToList();
        var erroresJson = entradas.Where(entrada => entrada.Key.StartsWith(PrefijoJson)).ToList();

        var errores = erroresJson.Count > 0
            ? erroresJson.ToDictionary(
                entrada => NombreCampoJson(entrada.Key),
                _ => new[] { "El formato del valor no es válido." })
            : entradas.ToDictionary(
                entrada => NombresCampos.ACamelCase(entrada.Key),
                entrada => entrada.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        contexto.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(RespuestaModeloInvalido))
            .LogInformation("Solicitud con formato no válido en {Campos}", errores.Keys);

        return new ResultadoProblema(Problemas.CrearValidacion(contexto.HttpContext, errores));
    }

    private static string NombreCampoJson(string clave)
    {
        var nombre = clave.TrimStart('$').TrimStart('.');
        return nombre.Length == 0 ? "cuerpo" : nombre;
    }
}
