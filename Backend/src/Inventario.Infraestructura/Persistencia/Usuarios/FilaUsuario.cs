using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Dominio.Usuarios;

namespace Inventario.Infraestructura.Persistencia.Usuarios;

internal sealed record FilaUsuario(
    int Id,
    string NombreUsuario,
    string Correo,
    string HashContrasena,
    string Rol,
    bool Activo,
    DateTime? ExpiraContrasena)
{
    public Usuario AUsuario() =>
        Usuario.Reconstruir(
            Id, NombreUsuario, Correo, HashContrasena, Enum.Parse<RolUsuario>(Rol), Activo, ACalendario(ExpiraContrasena));

    internal static DateOnly? ACalendario(DateTime? fecha) =>
        fecha is { } valor ? DateOnly.FromDateTime(valor) : null;
}

internal sealed record FilaUsuarioListado(
    int Id,
    string NombreUsuario,
    string Correo,
    string Rol,
    bool Activo,
    DateTime? ExpiraContrasena)
{
    public UsuarioDto ADto() =>
        new(Id, NombreUsuario, Correo, Rol, Activo, FilaUsuario.ACalendario(ExpiraContrasena));
}
