namespace Inventario.Dominio.Excepciones;

public sealed class ExcepcionCuentaPropia()
    : ExcepcionDominio("No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.");
