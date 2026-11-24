using Microsoft.Extensions.Configuration;

namespace Inventario.Infraestructura.Configuracion;

internal static class LectorConfiguracion
{
    public static string Requerido(this IConfiguration configuracion, string clave)
    {
        var valor = configuracion[clave];

        return string.IsNullOrWhiteSpace(valor)
            ? throw new ExcepcionConfiguracion($"Falta la variable {clave.Replace(":", "__")} en .env.")
            : valor;
    }

    public static int EnteroPositivo(this IConfiguration configuracion, string clave) =>
        int.TryParse(configuracion.Requerido(clave), out var valor) && valor > 0
            ? valor
            : throw new ExcepcionConfiguracion($"La variable {clave.Replace(":", "__")} debe ser un entero mayor a cero.");
}
