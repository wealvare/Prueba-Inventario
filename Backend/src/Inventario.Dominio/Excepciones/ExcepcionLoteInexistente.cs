namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionLoteInexistente()
    : ExcepcionDominio("Uno de los lotes ya no existe en el producto. Vuelve a cargarlo e inténtalo de nuevo.");
