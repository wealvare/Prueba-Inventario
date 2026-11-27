namespace Inventario.Aplicacion.Usuarios.Dtos;

public sealed record UsuarioDto(int Id, string NombreUsuario, string Correo, string Rol, bool Activo, DateOnly? ExpiraContrasena);
