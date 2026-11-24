using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Inventario.Infraestructura.Configuracion;

public sealed record OpcionesBaseDatos(string CadenaConexion)
{
    public static OpcionesBaseDatos Leer(IConfiguration configuracion) => new(new SqlConnectionStringBuilder
    {
        DataSource = $"{configuracion.Requerido("BaseDatos:Servidor")},{configuracion.EnteroPositivo("BaseDatos:Puerto")}",
        InitialCatalog = configuracion.Requerido("BaseDatos:Nombre"),
        UserID = configuracion.Requerido("BaseDatos:Usuario"),
        Password = configuracion.Requerido("BaseDatos:Contrasena"),
        TrustServerCertificate = string.Equals(
            configuracion["BaseDatos:ConfiarCertificado"], "true", StringComparison.OrdinalIgnoreCase)
    }.ConnectionString);
}
