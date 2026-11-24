using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Inventario.Api.Errores;

internal static class Problemas
{
    public const string TipoContenido = "application/problem+json";

    private static readonly Dictionary<int, (string Titulo, string Detalle)> Textos = new()
    {
        [400] = ("Solicitud no válida", "Revisa los datos enviados."),
        [401] = ("No autenticado", "Debes iniciar sesión con un token válido."),
        [403] = ("Acceso denegado", "No tienes permiso para realizar esta acción."),
        [404] = ("No encontrado", "El recurso solicitado no existe."),
        [405] = ("Método no permitido", "El recurso no admite este método."),
        [409] = ("Conflicto", "El recurso entra en conflicto con su estado actual."),
        [413] = ("Solicitud demasiado grande", "El contenido supera el tamaño permitido."),
        [415] = ("Formato no admitido", "El contenido debe enviarse en formato JSON."),
        [429] = ("Demasiadas peticiones", "Espera un momento antes de volver a intentarlo."),
        [500] = ("Error interno", "Ocurrió un error inesperado. Inténtalo de nuevo más tarde.")
    };

    public static ProblemDetails Crear(HttpContext contexto, int estado, string? detalle = null) =>
        Completar(new ProblemDetails(), contexto, estado, detalle);

    public static ValidationProblemDetails CrearValidacion(HttpContext contexto, IDictionary<string, string[]> errores) =>
        Completar(new ValidationProblemDetails(errores), contexto, StatusCodes.Status400BadRequest, null);

    public static Task EscribirAsync(HttpContext contexto, ProblemDetails problema)
    {
        contexto.Response.StatusCode = problema.Status!.Value;
        return contexto.Response.WriteAsJsonAsync(
            problema, problema.GetType(), options: null, TipoContenido, contexto.RequestAborted);
    }

    private static T Completar<T>(T problema, HttpContext contexto, int estado, string? detalle)
        where T : ProblemDetails
    {
        var (titulo, detalleGeneral) = Textos.GetValueOrDefault(
            estado, (ReasonPhrases.GetReasonPhrase(estado), "No se pudo completar la solicitud."));

        problema.Status = estado;
        problema.Title = titulo;
        problema.Detail = detalle ?? detalleGeneral;
        problema.Instance = contexto.Request.Path;
        problema.Extensions["correlacion"] = contexto.TraceIdentifier;
        return problema;
    }
}
