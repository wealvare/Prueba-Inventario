using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.Dtos;

public sealed record SolicitudCrearUsuario(
    string NombreUsuario,
    string Correo,
    string Contrasena,
    RolUsuario? Rol);
