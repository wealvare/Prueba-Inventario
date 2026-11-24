using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Mapeos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Excepciones;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class ObtenerUsuario(IRepositorioUsuarios repositorio) : IObtenerUsuario
{
    public async Task<UsuarioDto> EjecutarAsync(int id, CancellationToken cancelacion)
    {
        var usuario = await repositorio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new ExcepcionUsuarioNoEncontrado(id);

        return usuario.ADto();
    }
}
