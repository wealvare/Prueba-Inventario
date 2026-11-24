using FluentValidation;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Entrada;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Dominio.Excepciones;

namespace Inventario.Aplicacion.Autenticacion.CasosUso;

public sealed class IniciarSesion(
    IRepositorioUsuarios repositorio,
    IHasheadorContrasena hasheador,
    IServicioToken servicioToken,
    IValidator<SolicitudInicioSesion> validador) : IIniciarSesion
{
    public async Task<RespuestaInicioSesion> EjecutarAsync(SolicitudInicioSesion solicitud, CancellationToken cancelacion)
    {
        await validador.ValidateAndThrowAsync(solicitud, cancelacion);

        var usuario = await repositorio.ObtenerPorNombreUsuarioAsync(solicitud.NombreUsuario.Trim(), cancelacion);

        if (usuario is null || !hasheador.Verificar(solicitud.Contrasena, usuario.HashContrasena))
            throw new ExcepcionCredencialesInvalidas();

        var token = servicioToken.Generar(usuario);

        return new RespuestaInicioSesion(token.Valor, token.Expiracion, usuario.NombreUsuario, usuario.Rol.ToString());
    }
}
