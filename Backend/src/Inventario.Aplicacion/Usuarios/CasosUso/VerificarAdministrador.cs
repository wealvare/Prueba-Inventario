using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class VerificarAdministrador(IRepositorioUsuarios repositorio) : IVerificarAdministrador
{
    public async Task<bool> EjecutarAsync(int id, CancellationToken cancelacion) =>
        await repositorio.ObtenerPorIdAsync(id, cancelacion) is { EsAdministradorActivo: true };
}
