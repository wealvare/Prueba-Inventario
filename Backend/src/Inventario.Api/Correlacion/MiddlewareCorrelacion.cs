using System.Text.RegularExpressions;
using Inventario.Api.Registros;
using Serilog.Context;

namespace Inventario.Api.Correlacion;

public sealed partial class MiddlewareCorrelacion(RequestDelegate siguiente)
{
    public const string Encabezado = "X-Correlacion";

    public async Task InvokeAsync(HttpContext contexto)
    {
        var recibida = contexto.Request.Headers[Encabezado].ToString();
        var correlacion = FormatoValido().IsMatch(recibida) ? recibida : Guid.NewGuid().ToString("N");

        contexto.TraceIdentifier = correlacion;
        contexto.Response.Headers[Encabezado] = correlacion;

        using (LogContext.PushProperty(ConfiguracionRegistros.Correlacion, correlacion))
            await siguiente(contexto);
    }

    [GeneratedRegex(@"\A[A-Za-z0-9-]{1,64}\z")]
    private static partial Regex FormatoValido();
}
