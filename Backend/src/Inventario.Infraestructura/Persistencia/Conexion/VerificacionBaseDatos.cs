using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Inventario.Infraestructura.Persistencia.Conexion;

public sealed class VerificacionBaseDatos(IFabricaConexion fabrica) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conexion = await fabrica.AbrirAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar con la base de datos.", error);
        }
    }
}
