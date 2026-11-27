using FluentValidation;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Mapeos;
using Inventario.Aplicacion.Usuarios.Puertos.Entrada;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

public sealed class RegistrarUsuario(
    IRepositorioUsuarios repositorio,
    IHasheadorContrasena hasheador,
    IValidator<SolicitudRegistro> validador) : IRegistrarUsuario
{
    public async Task<UsuarioDto> EjecutarAsync(SolicitudRegistro solicitud, CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);
        PoliticaContrasena.Validar(solicitud.Contrasena);

        var usuario = new Usuario(
            solicitud.NombreUsuario,
            solicitud.Correo,
            hasheador.Hashear(solicitud.Contrasena),
            RolUsuario.Consulta,
            solicitud.ExpiraContrasena);

        await repositorio.ComprobarUnicoAsync(usuario, cancelacion);

        return (await repositorio.AgregarAsync(usuario, cancelacion)).ADto();
    }
}
