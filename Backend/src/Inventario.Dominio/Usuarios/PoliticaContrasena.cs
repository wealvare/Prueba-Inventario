using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Usuarios;

public static class PoliticaContrasena
{
    public const int LongitudMinima = 8;
    public const int LongitudMaxima = 100;

    public static readonly string Mensaje =
        $"La contraseña debe tener entre {LongitudMinima} y {LongitudMaxima} caracteres, con mayúscula, minúscula y número.";

    public static bool Cumple(string? contrasena) =>
        contrasena is { Length: >= LongitudMinima and <= LongitudMaxima }
        && contrasena.Any(char.IsUpper)
        && contrasena.Any(char.IsLower)
        && contrasena.Any(char.IsDigit);

    public static void Validar(string? contrasena)
    {
        if (!Cumple(contrasena))
            throw new ExcepcionReglaNegocio(Mensaje);
    }
}
