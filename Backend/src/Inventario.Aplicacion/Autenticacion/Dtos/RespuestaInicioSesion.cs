namespace Inventario.Aplicacion.Autenticacion.Dtos;

public sealed record RespuestaInicioSesion(string Token, DateTime Expiracion, string NombreUsuario, string Rol);
