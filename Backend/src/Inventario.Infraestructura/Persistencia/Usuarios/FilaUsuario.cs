using Inventario.Dominio.Usuarios;

namespace Inventario.Infraestructura.Persistencia.Usuarios;

internal sealed record FilaUsuario(int Id, string NombreUsuario, string Correo, string HashContrasena, string Rol)
{
    public Usuario AUsuario()
    {
        var usuario = new Usuario(NombreUsuario, Correo, HashContrasena, Enum.Parse<RolUsuario>(Rol));
        Rehidratacion.AsignarId(usuario, Id);
        return usuario;
    }
}
