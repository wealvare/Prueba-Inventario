using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Excepciones;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class DesactivarUsuario(IRepositorioUsuarios repositorio) : IDesactivarUsuario
{
    public async Task EjecutarAsync(int id, int idActor, CancellationToken cancelacion)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new ExcepcionUsuarioNoEncontrado(id);
        var eraAdministradorActivo = usuario.EsAdministradorActivo;

        usuario.Desactivar(idActor);
        await repositorio.ComprobarAdministradorRestanteAsync(usuario, eraAdministradorActivo, cancelacion);
        await repositorio.ActualizarAsync(usuario, cancelacion);
    }
}
