using System.Net.Mail;
using Inventario.Dominio.Comun;
using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Usuarios;

public sealed class Usuario
{
    public const int LongitudNombreUsuario = 50;
    public const int LongitudCorreo = 150;
    public const int LongitudHashContrasena = 500;

    public Usuario(string nombreUsuario, string correo, string hashContrasena, RolUsuario rol)
    {
        NombreUsuario = Texto.Requerido(nombreUsuario, LongitudNombreUsuario, "nombre de usuario");
        Correo = Texto.Requerido(correo, LongitudCorreo, "correo");
        HashContrasena = Texto.Requerido(hashContrasena, LongitudHashContrasena, "contraseña");

        if (!MailAddress.TryCreate(Correo, out _))
            throw new ExcepcionReglaNegocio("El correo no tiene un formato válido.");

        if (!Enum.IsDefined(rol))
            throw new ExcepcionReglaNegocio("El rol no es válido.");

        Rol = rol;
    }

    public int Id { get; private set; }
    public string NombreUsuario { get; private set; }
    public string Correo { get; private set; }
    public string HashContrasena { get; private set; }
    public RolUsuario Rol { get; private set; }

    public static Usuario Reconstruir(int id, string nombreUsuario, string correo, string hashContrasena, RolUsuario rol) =>
        new(nombreUsuario, correo, hashContrasena, rol) { Id = id };
}
