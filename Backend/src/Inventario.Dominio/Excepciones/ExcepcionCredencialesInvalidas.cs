namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionCredencialesInvalidas()
    : ExcepcionDominio("El usuario o la contraseña no son correctos.");
