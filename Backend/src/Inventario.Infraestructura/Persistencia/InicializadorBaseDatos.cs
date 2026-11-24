using Inventario.Infraestructura.Persistencia.DatosIniciales;

namespace Inventario.Infraestructura.Persistencia;

public sealed class InicializadorBaseDatos(MigradorEsquema migrador, CargadorDatosIniciales cargador)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        migrador.Ejecutar();
        await cargador.EjecutarAsync(cancelacion);
    }
}
