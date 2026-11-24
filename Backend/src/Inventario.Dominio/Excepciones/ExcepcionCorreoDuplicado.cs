namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionCorreoDuplicado(string correo)
    : ExcepcionDominio($"Ya existe un usuario con el correo '{correo}'.");
