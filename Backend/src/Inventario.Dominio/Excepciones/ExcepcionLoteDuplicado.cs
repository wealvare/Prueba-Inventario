namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionLoteDuplicado(string numeroLote)
    : ExcepcionDominio($"Ya existe un lote con el número '{numeroLote}' en el producto.");
