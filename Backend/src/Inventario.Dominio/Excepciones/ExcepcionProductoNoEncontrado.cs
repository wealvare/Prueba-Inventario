namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionProductoNoEncontrado(int id)
    : ExcepcionDominio($"No existe un producto con el id {id}.");
