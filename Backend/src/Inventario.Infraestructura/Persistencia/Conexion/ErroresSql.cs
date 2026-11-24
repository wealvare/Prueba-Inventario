using Microsoft.Data.SqlClient;

namespace Inventario.Infraestructura.Persistencia.Conexion;

internal static class ErroresSql
{
    private const int IndiceUnicoDuplicado = 2601;
    private const int RestriccionUnicaDuplicada = 2627;

    public static bool EsDuplicado(this SqlException error, string indice) =>
        error.Number is IndiceUnicoDuplicado or RestriccionUnicaDuplicada
        && error.Message.Contains(indice, StringComparison.OrdinalIgnoreCase);
}
