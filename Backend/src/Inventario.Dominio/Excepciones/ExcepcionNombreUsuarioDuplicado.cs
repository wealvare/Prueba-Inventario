namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionNombreUsuarioDuplicado(string nombreUsuario)
    : ExcepcionDominio($"Ya existe un usuario con el nombre '{nombreUsuario}'.");
