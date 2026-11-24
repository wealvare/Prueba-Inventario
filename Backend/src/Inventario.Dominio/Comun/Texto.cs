using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Comun;

internal static class Texto
{
    public static string Requerido(string? valor, int longitudMaxima, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ExcepcionReglaNegocio($"El campo {campo} es obligatorio.");

        return Limitado(valor, longitudMaxima, campo);
    }

    public static string Limitado(string? valor, int longitudMaxima, string campo)
    {
        var texto = valor?.Trim() ?? string.Empty;

        if (texto.Length > longitudMaxima)
            throw new ExcepcionReglaNegocio($"El campo {campo} no puede superar los {longitudMaxima} caracteres.");

        return texto;
    }
}
