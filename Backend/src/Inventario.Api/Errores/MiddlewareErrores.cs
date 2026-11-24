using FluentValidation;
using Inventario.Dominio.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Api.Errores;

public sealed class MiddlewareErrores(RequestDelegate siguiente, ILogger<MiddlewareErrores> registro)
{
    private const int SolicitudCancelada = 499;

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            registro.LogInformation("El cliente canceló la solicitud");

            if (!contexto.Response.HasStarted)
                contexto.Response.StatusCode = SolicitudCancelada;
        }
        catch (Exception error) when (!contexto.Response.HasStarted)
        {
            var problema = Convertir(contexto, error);
            Registrar(error, problema.Status!.Value);
            await Problemas.EscribirAsync(contexto, problema);
        }
    }

    private static ProblemDetails Convertir(HttpContext contexto, Exception error) => error switch
    {
        ValidationException validacion => Problemas.CrearValidacion(contexto, Agrupar(validacion)),
        ExcepcionCodigoDuplicado or ExcepcionLoteDuplicado or ExcepcionLoteInexistente =>
            Problemas.Crear(contexto, StatusCodes.Status409Conflict, error.Message),
        ExcepcionProductoNoEncontrado => Problemas.Crear(contexto, StatusCodes.Status404NotFound, error.Message),
        ExcepcionCredencialesInvalidas => Problemas.Crear(contexto, StatusCodes.Status401Unauthorized, error.Message),
        ExcepcionDominio => Problemas.Crear(contexto, StatusCodes.Status400BadRequest, error.Message),
        BadHttpRequestException solicitud => Problemas.Crear(contexto, solicitud.StatusCode),
        _ => Problemas.Crear(contexto, StatusCodes.Status500InternalServerError)
    };

    private static Dictionary<string, string[]> Agrupar(ValidationException error) => error.Errors
        .GroupBy(fallo => NombresCampos.ACamelCase(fallo.PropertyName))
        .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(fallo => fallo.ErrorMessage).ToArray());

    private void Registrar(Exception error, int estado)
    {
        if (estado >= StatusCodes.Status500InternalServerError)
            registro.LogError(error, "Error no controlado");
        else if (error is ExcepcionCredencialesInvalidas)
            registro.LogWarning("Inicio de sesión rechazado");
        else
            registro.LogInformation("Solicitud rechazada con estado {Estado} por {Tipo}", estado, error.GetType().Name);
    }
}
