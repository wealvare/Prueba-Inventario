using System.Text.RegularExpressions;

namespace Inventario.Aplicacion.RegistrosCliente.CasosUso;

internal static partial class DatosSensibles
{
    private const string Oculto = "[oculto]";

    public static string Ocultar(string texto)
    {
        texto = CaracteresControl().Replace(texto, " ");
        texto = Portador().Replace(texto, Oculto);
        texto = Credencial().Replace(texto, Oculto);
        texto = Consulta().Replace(texto, Oculto);
        texto = Correo().Replace(texto, Oculto);
        texto = HashBcrypt().Replace(texto, Oculto);
        texto = TextoOpaco().Replace(texto, Oculto);
        return NumeroLargo().Replace(texto, Oculto).Trim();
    }

    [GeneratedRegex(@"[\p{C}\p{Zl}\p{Zp}]+")]
    private static partial Regex CaracteresControl();

    [GeneratedRegex(@"\bBearer\s+\S+", RegexOptions.IgnoreCase)]
    private static partial Regex Portador();

    [GeneratedRegex(
        @"\b(contrase(n|ñ|%C3%B1)a|password|passwd|clave|token|secret|authorization)\b[""']?\s*[:=]\s*(""[^""]*""?|'[^']*'?|[^\s,;]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex Credencial();

    [GeneratedRegex(@"[?#][^\s=]+=\S*")]
    private static partial Regex Consulta();

    [GeneratedRegex(@"[^\s@""'<>(),;:/]+(@|%40)[^\s@""'<>(),;:/]+", RegexOptions.IgnoreCase)]
    private static partial Regex Correo();

    [GeneratedRegex(@"\$2[abxy]?\$\d{2}\$[./A-Za-z0-9]+")]
    private static partial Regex HashBcrypt();

    [GeneratedRegex(@"[A-Za-z0-9_-]{32,}(\.[A-Za-z0-9_-]+)*|eyJ[A-Za-z0-9_-]+(\.[A-Za-z0-9_-]+)*")]
    private static partial Regex TextoOpaco();

    [GeneratedRegex(@"\d{7,}")]
    private static partial Regex NumeroLargo();
}
