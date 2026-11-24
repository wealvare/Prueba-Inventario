using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Dtos;

public sealed record SolicitudActualizarUsuario(
    string NombreUsuario,
    string Correo,
    RolUsuario? Rol,
    bool? Activo,
    string? Contrasena);
