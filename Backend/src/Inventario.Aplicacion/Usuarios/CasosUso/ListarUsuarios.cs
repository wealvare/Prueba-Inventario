using FluentValidation;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class ListarUsuarios(
    IRepositorioUsuarios repositorio,
    IValidator<FiltroUsuarios> validador) : IListarUsuarios
{
    public async Task<ResultadoPaginado<UsuarioDto>> EjecutarAsync(
        FiltroUsuarios filtro,
        CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(filtro, cancelacion);

        return await repositorio.ListarAsync(filtro, cancelacion);
    }
}
