using Inventario.Dominio.Usuarios;
using Microsoft.Extensions.Configuration;

namespace Inventario.Infraestructura.Configuracion;

public sealed record UsuarioSemilla(string NombreUsuario, string Correo, string Contrasena, RolUsuario Rol);

public sealed record OpcionesSemilla(IReadOnlyList<UsuarioSemilla> Usuarios)
{
    private const int LongitudMinimaContrasena = 8;

    public static OpcionesSemilla Leer(IConfiguration configuracion) => new(
    [
        LeerUsuario(configuracion, "Administrador", RolUsuario.Administrador),
        LeerUsuario(configuracion, "Consulta", RolUsuario.Consulta)
    ]);

    private static UsuarioSemilla LeerUsuario(IConfiguration configuracion, string seccion, RolUsuario rol)
    {
        var contrasena = configuracion.Requerido($"Semilla:{seccion}:Contrasena");

        if (contrasena.Length < LongitudMinimaContrasena)
            throw new InvalidOperationException(
                $"La contraseña del usuario semilla {seccion} debe tener al menos {LongitudMinimaContrasena} caracteres.");

        return new UsuarioSemilla(
            configuracion.Requerido($"Semilla:{seccion}:NombreUsuario"),
            configuracion.Requerido($"Semilla:{seccion}:Correo"),
            contrasena,
            rol);
    }
}
