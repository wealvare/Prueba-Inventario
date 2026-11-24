using Inventario.Dominio.Usuarios;
using Microsoft.Extensions.Configuration;

namespace Inventario.Infraestructura.Configuracion;

public sealed record UsuarioInicial(string NombreUsuario, string Correo, string Contrasena, RolUsuario Rol);

public sealed record OpcionesUsuariosIniciales(IReadOnlyList<UsuarioInicial> Usuarios)
{
    private const string Seccion = "UsuariosIniciales";
    private const int LongitudMinimaContrasena = 8;

    private static readonly RolUsuario[] Roles = [RolUsuario.Administrador, RolUsuario.Consulta];
    private static readonly string[] Campos =
        [nameof(UsuarioInicial.NombreUsuario), nameof(UsuarioInicial.Correo), nameof(UsuarioInicial.Contrasena)];

    public static OpcionesUsuariosIniciales Leer(IConfiguration configuracion)
    {
        ComprobarVariables(configuracion);

        return new OpcionesUsuariosIniciales([.. Roles.Select(rol => LeerUsuario(configuracion, rol))]);
    }

    private static void ComprobarVariables(IConfiguration configuracion)
    {
        var faltantes = Roles
            .SelectMany(rol => Campos.Select(campo => Clave(rol, campo)))
            .Where(clave => string.IsNullOrWhiteSpace(configuracion[clave]))
            .Select(clave => clave.Replace(":", "__"))
            .ToList();

        if (faltantes.Count > 0)
            throw new ExcepcionConfiguracion(
                $"Faltan estas variables en .env: {string.Join(", ", faltantes)}. "
                + "Si tu .env es anterior al cambio de nombres, renómbralas según .env.example.");
    }

    private static UsuarioInicial LeerUsuario(IConfiguration configuracion, RolUsuario rol)
    {
        var contrasena = Valor(nameof(UsuarioInicial.Contrasena));

        if (contrasena.Length < LongitudMinimaContrasena)
            throw new ExcepcionConfiguracion(
                $"La contraseña del usuario inicial {rol} debe tener al menos {LongitudMinimaContrasena} caracteres.");

        return new UsuarioInicial(
            Valor(nameof(UsuarioInicial.NombreUsuario)), Valor(nameof(UsuarioInicial.Correo)), contrasena, rol);

        string Valor(string campo) => configuracion.Requerido(Clave(rol, campo));
    }

    private static string Clave(RolUsuario rol, string campo) => $"{Seccion}:{rol}:{campo}";
}
