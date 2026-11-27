using System.Text.RegularExpressions;
using Inventario.Dominio.Comun;
using Inventario.Dominio.Excepciones;

namespace Inventario.Dominio.Usuarios;

public sealed partial class Usuario
{
    public const int LongitudMinimaNombreUsuario = 3;
    public const int LongitudNombreUsuario = 50;
    public const int LongitudCorreo = 150;
    public const int LongitudHashContrasena = 500;

    public const string MensajeNombreUsuario =
        "El nombre de usuario debe tener al menos 3 caracteres: letras sin tilde, números, punto, guion o guion bajo.";

    public const string MensajeCorreo = "El correo no tiene un formato válido.";

    public Usuario(string nombreUsuario, string correo, string hashContrasena, RolUsuario rol, DateOnly? expiraContrasena = null)
        : this(
            0,
            ValidarNombreUsuario(nombreUsuario),
            ValidarCorreo(correo),
            ValidarHash(hashContrasena),
            ValidarRol(rol),
            true,
            expiraContrasena)
    {
    }

    private Usuario(int id, string nombreUsuario, string correo, string hashContrasena, RolUsuario rol, bool activo, DateOnly? expiraContrasena)
    {
        Id = id;
        NombreUsuario = nombreUsuario;
        Correo = correo;
        HashContrasena = hashContrasena;
        Rol = rol;
        Activo = activo;
        ExpiraContrasena = expiraContrasena;
    }

    public int Id { get; private set; }
    public string NombreUsuario { get; private set; }
    public string Correo { get; private set; }
    public string HashContrasena { get; private set; }
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateOnly? ExpiraContrasena { get; private set; }

    public bool EsAdministradorActivo => Activo && Rol == RolUsuario.Administrador;

    public bool TieneVigencia(DateOnly hoy) => ExpiraContrasena is { } fecha && fecha >= hoy;

    public static Usuario Reconstruir(
        int id,
        string nombreUsuario,
        string correo,
        string hashContrasena,
        RolUsuario rol,
        bool activo,
        DateOnly? expiraContrasena = null) =>
        new(id, nombreUsuario, correo, hashContrasena, rol, activo, expiraContrasena);

    public static bool EsNombreUsuarioValido(string nombreUsuario) =>
        nombreUsuario.Trim() is { Length: >= LongitudMinimaNombreUsuario } nombre && FormatoNombreUsuario().IsMatch(nombre);

    public static bool EsCorreoValido(string correo) => FormatoCorreo().IsMatch(correo.Trim());

    public void Actualizar(
        string nombreUsuario,
        string correo,
        RolUsuario rol,
        bool activo,
        DateOnly? expiraContrasena,
        int idActor)
    {
        if (Id == idActor && (rol != RolUsuario.Administrador || !activo))
            throw new ExcepcionCuentaPropia();

        NombreUsuario = ValidarNombreUsuario(nombreUsuario);
        Correo = ValidarCorreo(correo);
        Rol = ValidarRol(rol);
        Activo = activo;
        ExpiraContrasena = expiraContrasena;
    }

    public void CambiarContrasena(string hashContrasena) => HashContrasena = ValidarHash(hashContrasena);

    public void Desactivar(int idActor)
    {
        if (Id == idActor)
            throw new ExcepcionCuentaPropia();

        Activo = false;
    }

    private static string ValidarNombreUsuario(string? nombreUsuario)
    {
        var nombre = Texto.Requerido(nombreUsuario, LongitudNombreUsuario, "nombre de usuario");

        return EsNombreUsuarioValido(nombre) ? nombre : throw new ExcepcionReglaNegocio(MensajeNombreUsuario);
    }

    private static string ValidarCorreo(string? correo)
    {
        var texto = Texto.Requerido(correo, LongitudCorreo, "correo");

        return EsCorreoValido(texto) ? texto : throw new ExcepcionReglaNegocio(MensajeCorreo);
    }

    private static string ValidarHash(string? hashContrasena) =>
        Texto.Requerido(hashContrasena, LongitudHashContrasena, "contraseña");

    private static RolUsuario ValidarRol(RolUsuario rol) =>
        Enum.IsDefined(rol) ? rol : throw new ExcepcionReglaNegocio("El rol no es válido.");

    [GeneratedRegex(@"\A[A-Za-z0-9._-]+\z")]
    private static partial Regex FormatoNombreUsuario();

    [GeneratedRegex(@"\A[^\s@]+@[^\s@]+\.[^\s@]+\z")]
    private static partial Regex FormatoCorreo();
}
