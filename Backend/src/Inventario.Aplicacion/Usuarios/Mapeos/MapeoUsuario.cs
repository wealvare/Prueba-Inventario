using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Mapeos;

internal static class MapeoUsuario
{
    public static UsuarioDto ADto(this Usuario usuario) =>
        new(usuario.Id, usuario.NombreUsuario, usuario.Correo, usuario.Rol.ToString(), usuario.Activo, usuario.ExpiraContrasena);
}
