using Inventario.Api.Correlacion;
using Inventario.Api.Errores;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

namespace Inventario.Api;

public static class CanalizacionApi
{
    public static WebApplication UsarApi(this WebApplication aplicacion)
    {
        aplicacion.UseResponseCompression();
        aplicacion.UseMiddleware<MiddlewareCorrelacion>();
        aplicacion.UseSerilogRequestLogging(opciones =>
        {
            opciones.MessageTemplate = "{RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0} ms";
            opciones.GetLevel = (contexto, _, _) => NivelRegistro(contexto.Response.StatusCode);
        });
        aplicacion.UseMiddleware<MiddlewareErrores>();
        aplicacion.UseStatusCodePages(pagina => Problemas.EscribirAsync(
            pagina.HttpContext,
            Problemas.Crear(pagina.HttpContext, pagina.HttpContext.Response.StatusCode)));

        if (aplicacion.Environment.IsDevelopment())
        {
            aplicacion.UseSwagger(opciones => opciones.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
            aplicacion.UseSwaggerUI(opciones => opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventario v1"));
        }

        aplicacion.UseRouting();
        aplicacion.UseCors(RegistroApi.PoliticaCors);
        aplicacion.UseAuthentication();
        aplicacion.UseRateLimiter();
        aplicacion.UseAuthorization();
        aplicacion.MapControllers();
        aplicacion.MapHealthChecks("/salud");

        return aplicacion;
    }

    private static LogEventLevel NivelRegistro(int estado) => estado switch
    {
        >= StatusCodes.Status500InternalServerError => LogEventLevel.Error,
        >= StatusCodes.Status400BadRequest => LogEventLevel.Warning,
        _ => LogEventLevel.Information
    };
}
