using System.Net;
using System.Text.Json;

namespace Inventario.Pruebas.Api;

internal static class Respuestas
{
    public static async Task<JsonElement> ProblemaAsync(this HttpResponseMessage respuesta, HttpStatusCode estado)
    {
        var contenido = await respuesta.Content.ReadAsStringAsync();

        Assert.True(estado == respuesta.StatusCode, $"{respuesta.StatusCode}: {contenido}");
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = JsonDocument.Parse(contenido).RootElement;
        Assert.Equal((int)estado, problema.GetProperty("status").GetInt32());
        Assert.Equal(
            Assert.Single(respuesta.Headers.GetValues("X-Correlacion")),
            problema.GetProperty("correlacion").GetString());
        return problema;
    }

    public static string? Detalle(this JsonElement problema) => problema.GetProperty("detail").GetString();

    public static string[] Errores(this JsonElement problema, string campo) => problema
        .GetProperty("errors").GetProperty(campo).EnumerateArray().Select(error => error.GetString()!).ToArray();
}
