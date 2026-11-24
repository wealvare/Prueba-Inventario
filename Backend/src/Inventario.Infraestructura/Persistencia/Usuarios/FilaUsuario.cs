using Inventario.Dominio.Usuarios;

namespace Inventario.Infraestructura.Persistencia.Usuarios;

internal sealed record FilaUsuario(
    int Id,
    string NombreUsuario,
    string Correo,
    string HashContrasena,
    string Rol,
    bool Activo)
{
    public Usuario AUsuario() =>
        Usuario.Reconstruir(Id, NombreUsuario, Correo, HashContrasena, Enum.Parse<RolUsuario>(Rol), Activo);
}
