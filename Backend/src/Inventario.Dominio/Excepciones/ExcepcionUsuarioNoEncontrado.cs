namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionUsuarioNoEncontrado(int id)
    : ExcepcionDominio($"No existe un usuario con el id {id}.");
