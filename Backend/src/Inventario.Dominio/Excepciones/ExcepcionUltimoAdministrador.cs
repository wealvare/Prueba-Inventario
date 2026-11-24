namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionUltimoAdministrador()
    : ExcepcionDominio("El sistema debe conservar al menos un administrador activo.");
