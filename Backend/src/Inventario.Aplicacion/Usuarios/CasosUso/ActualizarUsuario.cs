using FluentValidation;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Mapeos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class ActualizarUsuario(
    IRepositorioUsuarios repositorio,
    IHasheadorContrasena hasheador,
    IValidator<SolicitudActualizarUsuario> validador) : IActualizarUsuario
{
    public async Task<UsuarioDto> EjecutarAsync(
        int id,
        SolicitudActualizarUsuario solicitud,
        int idActor,
        CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);

        var usuario = await repositorio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new ExcepcionUsuarioNoEncontrado(id);
        var eraAdministradorActivo = usuario.EsAdministradorActivo;

        usuario.Actualizar(
            solicitud.NombreUsuario,
            solicitud.Correo,
            solicitud.Rol!.Value,
            solicitud.Activo!.Value,
            idActor);

        await repositorio.ComprobarUnicoAsync(usuario, cancelacion);
        await repositorio.ComprobarAdministradorRestanteAsync(usuario, eraAdministradorActivo, cancelacion);

        if (solicitud.Contrasena is not null)
        {
            PoliticaContrasena.Validar(solicitud.Contrasena);
            usuario.CambiarContrasena(hasheador.Hashear(solicitud.Contrasena));
        }

        return (await repositorio.ActualizarAsync(usuario, cancelacion)).ADto();
    }
}
