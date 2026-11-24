using Inventario.Infraestructura.Persistencia.Semilla;

namespace Inventario.Infraestructura.Persistencia;

public sealed class InicializadorBaseDatos(MigradorEsquema migrador, SembradorDatos sembrador)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        migrador.Ejecutar();
        await sembrador.EjecutarAsync(cancelacion);
    }
}
