using System.Text.Json;

namespace Inventario.Api.Errores;

internal static class NombresCampos
{
    public static string ACamelCase(string ruta) =>
        string.Join('.', ruta.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
